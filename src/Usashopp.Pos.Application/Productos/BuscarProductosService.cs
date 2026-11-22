using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Services;

namespace Usashopp.Pos.Application.Productos;

/// <summary>
/// Búsqueda de productos para el POS. Primero intenta coincidencia exacta por código
/// de barras / SKU (uso con lector) y, si no, hace búsqueda por texto libre. Para el grid táctil
/// agrupa por PRODUCTO (una tarjeta por prenda) y arma la tabla talla × color de sus variantes.
/// </summary>
public class BuscarProductosService
{
    private readonly IVarianteRepository _variantes;
    private readonly IAlmacenImagenes _imagenes;

    public BuscarProductosService(IVarianteRepository variantes, IAlmacenImagenes imagenes)
    {
        _variantes = variantes;
        _imagenes = imagenes;
    }

    public async Task<ProductoBusquedaDto?> PorCodigoBarrasAsync(string codigo, CancellationToken ct = default)
    {
        var variante = await _variantes.ObtenerPorCodigoBarrasAsync(codigo, ct)
                       ?? await _variantes.ObtenerPorSkuAsync(codigo, ct); // el SKU impreso también sirve
        return variante is null ? null : Mapear(variante);
    }

    public async Task<IReadOnlyList<ProductoBusquedaDto>> PorTextoAsync(string texto, int limite = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return Array.Empty<ProductoBusquedaDto>();

        var variantes = await _variantes.BuscarAsync(texto.Trim(), limite, ct);
        return variantes.Select(Mapear).ToList();
    }

    /// <summary>Listado de variantes (selectores de apartados/compras), opcionalmente por categoría.</summary>
    public async Task<IReadOnlyList<ProductoBusquedaDto>> ParaGridAsync(
        Guid? categoriaId = null, int limite = 200, CancellationToken ct = default)
    {
        var variantes = await _variantes.ListarParaVentaAsync(categoriaId, limite, ct);
        return variantes.Select(Mapear).ToList();
    }

    /// <summary>
    /// Grid del POS: UNA tarjeta por producto (antes una por variante con tope de 200, así una
    /// playera en 5 tallas × 4 colores eran 20 tarjetas casi iguales). Paginado.
    /// </summary>
    public async Task<PaginaProductosGridDto> ParaGridProductosAsync(
        Guid? categoriaId = null, int saltar = 0, int tomar = 60, CancellationToken ct = default)
    {
        var variantes = await _variantes.ListarParaVentaAsync(categoriaId, int.MaxValue, ct);
        var productos = variantes
            .GroupBy(v => v.ProductoId)
            .Select(g =>
            {
                var lista = g.ToList();
                var p = lista[0].Producto;
                return new ProductoGridDto(
                    g.Key,
                    p?.Nombre ?? lista[0].DescripcionCompleta,
                    _imagenes.ObtenerRutaCompleta(p?.ImagenRuta),
                    lista.Min(v => v.PrecioVenta.Monto),
                    lista.Max(v => v.PrecioVenta.Monto),
                    lista.Sum(v => Math.Max(0, v.StockActual)),
                    lista.Count,
                    lista.All(v => v.EstaBajoMinimo),
                    lista.Count == 1 ? Mapear(lista[0]) : null);
            })
            .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new PaginaProductosGridDto(productos.Skip(saltar).Take(tomar).ToList(), productos.Count);
    }

    /// <summary>Tabla talla × color de un producto (filas = colores, columnas = tallas en orden lógico).</summary>
    public async Task<MatrizVariantesDto?> ObtenerMatrizAsync(Guid productoId, CancellationToken ct = default)
    {
        var variantes = (await _variantes.ListarParaVentaAsync(null, int.MaxValue, ct))
            .Where(v => v.ProductoId == productoId).ToList();
        if (variantes.Count == 0) return null;

        static string Texto(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
        var tallas = OrdenTallas.Ordenar(variantes.Select(v => Texto(v.Talla)).Distinct(StringComparer.OrdinalIgnoreCase)).ToList();
        var colores = variantes.Select(v => Texto(v.Color)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase).ToList();

        var filas = colores.Select(color => (IReadOnlyList<CeldaVarianteDto>)tallas.Select(talla =>
        {
            var v = variantes.FirstOrDefault(x =>
                string.Equals(Texto(x.Talla), talla, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Texto(x.Color), color, StringComparison.OrdinalIgnoreCase));
            return new CeldaVarianteDto(talla, color, v is null ? null : Mapear(v));
        }).ToList()).ToList();

        var p = variantes[0].Producto;
        return new MatrizVariantesDto(productoId, p?.Nombre ?? variantes[0].DescripcionCompleta,
            _imagenes.ObtenerRutaCompleta(p?.ImagenRuta), tallas, colores, filas);
    }

    private ProductoBusquedaDto Mapear(VarianteProducto v) => new(
        v.Id,
        v.DescripcionCompleta,
        v.Sku.Valor,
        v.CodigoBarras?.Valor,
        v.PrecioVenta.Monto,
        v.StockActual,
        v.EstaBajoMinimo,
        _imagenes.ObtenerRutaCompleta(v.Producto?.ImagenRuta));
}
