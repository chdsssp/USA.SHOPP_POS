using System.Globalization;
using System.Text;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Catalogo;

/// <summary>
/// Exporta el catálogo a CSV e importa/actualiza productos y variantes desde CSV.
/// Import: por cada fila, si el SKU existe se actualiza la variante; si no, se crea (agrupando
/// variantes bajo un producto por nombre+marca). La categoría se busca por nombre y se crea si falta.
/// El stock solo se fija al crear variantes nuevas (las existentes se ajustan por otros medios).
/// </summary>
public class CatalogoCsvService
{
    private const string Encabezado = "Producto,Marca,Categoria,Descripcion,SKU,CodigoBarras,Talla,Color,Precio,Costo,Stock,StockMinimo";

    private readonly IVarianteRepository _variantes;
    private readonly IProductoRepository _productos;
    private readonly IRepository<Categoria> _categorias;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public CatalogoCsvService(
        IVarianteRepository variantes,
        IProductoRepository productos,
        IRepository<Categoria> categorias,
        IMovimientoInventarioRepository movimientos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _variantes = variantes;
        _productos = productos;
        _categorias = categorias;
        _movimientos = movimientos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<string> ExportarAsync(CancellationToken ct = default)
    {
        var variantes = await _variantes.ListarInventarioAsync(null, false, true, ct);
        var ci = CultureInfo.InvariantCulture;

        var sb = new StringBuilder();
        sb.AppendLine(Encabezado);
        foreach (var v in variantes.OrderBy(x => x.Producto?.Nombre).ThenBy(x => x.Talla))
        {
            sb.AppendLine(string.Join(",",
                CsvUtil.Escapar(v.Producto?.Nombre),
                CsvUtil.Escapar(v.Producto?.Marca),
                CsvUtil.Escapar(v.Producto?.Categoria?.Nombre),
                CsvUtil.Escapar(v.Producto?.Descripcion),
                CsvUtil.Escapar(v.Sku.Valor),
                CsvUtil.Escapar(v.CodigoBarras?.Valor),
                CsvUtil.Escapar(v.Talla),
                CsvUtil.Escapar(v.Color),
                v.PrecioVenta.Monto.ToString(ci),
                v.Costo.Monto.ToString(ci),
                v.StockActual.ToString(ci),
                v.StockMinimo.ToString(ci)));
        }
        return sb.ToString();
    }

    public async Task<Result<ResultadoImportacionDto>> ImportarAsync(string contenido, CancellationToken ct = default)
    {
        var filas = CsvUtil.Parsear(contenido ?? string.Empty);
        if (filas.Count < 2)
            return Result.Falla<ResultadoImportacionDto>("El archivo no tiene datos (encabezado + al menos una fila).");

        var enc = filas[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(params string[] nombres) => enc.FindIndex(h => nombres.Contains(h));

        int iProd = Col("producto", "nombre"), iMarca = Col("marca"),
            iCat = Col("categoria", "categoría"), iDesc = Col("descripcion", "descripción"),
            iSku = Col("sku"), iCod = Col("codigobarras", "cod. barras", "codigo", "código"),
            iTalla = Col("talla"), iColor = Col("color"), iPrecio = Col("precio"),
            iCosto = Col("costo"), iStock = Col("stock", "existencia"),
            iStockMin = Col("stockminimo", "stock minimo", "mínimo", "minimo");

        if (iProd < 0 || iPrecio < 0)
            return Result.Falla<ResultadoImportacionDto>("El CSV debe incluir al menos las columnas «Producto» y «Precio».");

        var errores = new List<string>();
        int creados = 0, actualizados = 0, omitidos = 0;
        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        var categorias = (await _categorias.ListarAsync(null, ct)).ToList();
        var productos = (await _productos.ListarAsync(null, ct)).ToList();

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            for (var r = 1; r < filas.Count; r++)
            {
                var fila = filas[r];
                var nombreProd = Campo(fila, iProd);
                if (string.IsNullOrWhiteSpace(nombreProd)) { errores.Add($"Fila {r + 1}: nombre de producto vacío."); omitidos++; continue; }
                if (!TryDecimal(Campo(fila, iPrecio), out var precio)) { errores.Add($"Fila {r + 1}: precio inválido."); omitidos++; continue; }

                TryDecimal(Campo(fila, iCosto), out var costo);
                int.TryParse(Campo(fila, iStock), out var stock);
                int.TryParse(Campo(fila, iStockMin), out var stockMin);
                var talla = NullSiVacio(Campo(fila, iTalla));
                var color = NullSiVacio(Campo(fila, iColor));
                var codigo = NullSiVacio(Campo(fila, iCod));
                var sku = Campo(fila, iSku).Trim().ToUpperInvariant();

                // Actualización de una variante existente (por SKU).
                if (!string.IsNullOrWhiteSpace(sku) && await _variantes.ObtenerPorSkuAsync(sku, ct) is { } existente)
                {
                    existente.PrecioVenta = new Dinero(precio);
                    existente.Costo = new Dinero(costo);
                    existente.StockMinimo = stockMin;
                    existente.Talla = talla;
                    existente.Color = color;
                    existente.CodigoBarras = codigo is null ? null : new CodigoBarras(codigo);
                    _variantes.Actualizar(existente);
                    actualizados++;
                    continue;
                }

                // Alta: resolver categoría y producto (por nombre + marca).
                var marca = NullSiVacio(Campo(fila, iMarca));
                var categoria = ResolverCategoria(categorias, Campo(fila, iCat), out var creada);
                if (creada) await _categorias.AgregarAsync(categoria, ct);

                var producto = productos.FirstOrDefault(p =>
                    p.Nombre.Equals(nombreProd, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Marca ?? "", marca ?? "", StringComparison.OrdinalIgnoreCase));
                if (producto is null)
                {
                    producto = new Producto
                    {
                        Nombre = nombreProd,
                        Marca = marca,
                        Descripcion = NullSiVacio(Campo(fila, iDesc)),
                        CategoriaId = categoria.Id
                    };
                    await _productos.AgregarAsync(producto, ct);
                    productos.Add(producto);
                }

                var skuFinal = string.IsNullOrWhiteSpace(sku) ? GeneradorCodigos.NuevoSku() : sku;
                var variante = new VarianteProducto
                {
                    ProductoId = producto.Id,
                    Sku = new Sku(skuFinal),
                    CodigoBarras = codigo is null ? null : new CodigoBarras(codigo),
                    Talla = talla,
                    Color = color,
                    PrecioVenta = new Dinero(precio),
                    Costo = new Dinero(costo),
                    StockMinimo = stockMin
                };
                variante.EstablecerStock(stock);
                await _variantes.AgregarAsync(variante, ct);

                if (stock != 0)
                    await _movimientos.AgregarAsync(new MovimientoInventario
                    {
                        VarianteId = variante.Id,
                        Tipo = TipoMovimientoInventario.InventarioInicial,
                        Cantidad = stock,
                        Motivo = "Importación CSV",
                        UsuarioId = usuarioId,
                        Fecha = _reloj.UtcAhora
                    }, ct);
                creados++;
            }

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        await _auditoria.RegistrarAsync(
            "Importación de catálogo (CSV)",
            $"{creados} creados, {actualizados} actualizados, {omitidos} omitidos");

        return Result.Ok(new ResultadoImportacionDto(creados, actualizados, omitidos, errores));
    }

    private static Categoria ResolverCategoria(List<Categoria> cache, string nombre, out bool creada)
    {
        var buscado = string.IsNullOrWhiteSpace(nombre) ? "General" : nombre.Trim();
        var existente = cache.FirstOrDefault(c => c.Nombre.Equals(buscado, StringComparison.OrdinalIgnoreCase));
        if (existente is not null) { creada = false; return existente; }

        var nueva = new Categoria { Nombre = buscado };
        cache.Add(nueva);
        creada = true;
        return nueva;
    }

    private static string Campo(string[] fila, int idx) => idx >= 0 && idx < fila.Length ? fila[idx].Trim() : string.Empty;
    private static string? NullSiVacio(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool TryDecimal(string s, out decimal valor) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out valor) ||
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("es-MX"), out valor);
}
