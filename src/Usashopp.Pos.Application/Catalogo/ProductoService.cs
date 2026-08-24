using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Catalogo;

/// <summary>Alta y edición de productos con sus variantes (talla/color).</summary>
public class ProductoService
{
    private readonly IProductoRepository _productos;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public ProductoService(
        IProductoRepository productos,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _productos = productos;
        _variantes = variantes;
        _movimientos = movimientos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    /// <summary>Carga un producto con sus variantes para editarlo.</summary>
    public async Task<ProductoEdicionDto?> ObtenerParaEdicionAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _productos.ObtenerConVariantesAsync(id, ct);
        if (p is null) return null;
        return new ProductoEdicionDto(
            p.Id, p.Nombre, p.Descripcion, p.Marca, p.CategoriaId,
            p.Variantes.OrderBy(v => v.Talla).Select(v => new VarianteEntradaDto(
                v.Sku.Valor, v.CodigoBarras?.Valor, v.Talla, v.Color,
                v.PrecioVenta.Monto, v.Costo.Monto, v.StockActual, v.StockMinimo, v.Id)).ToList());
    }

    public async Task<Result> ActualizarAsync(Guid id, NuevoProductoDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla("El nombre del producto es obligatorio.");
        if (dto.Variantes.Count == 0)
            return Result.Falla("El producto debe tener al menos una variante.");

        var skusLote = dto.Variantes.Select(v => v.Sku.Trim().ToUpperInvariant()).ToList();
        if (skusLote.Distinct().Count() != skusLote.Count)
            return Result.Falla("Hay SKU repetidos entre las variantes.");
        foreach (var v in dto.Variantes)
            if (await _variantes.ExisteSkuAsync(v.Sku.Trim().ToUpperInvariant(), v.Id, ct))
                return Result.Falla($"El SKU «{v.Sku}» ya existe en otro producto.");

        var producto = await _productos.ObtenerConVariantesAsync(id, ct);
        if (producto is null) return Result.Falla("El producto no existe.");

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = dto.Descripcion;
        producto.Marca = dto.Marca;
        producto.CategoriaId = dto.CategoriaId;

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            _productos.Actualizar(producto);

            foreach (var v in dto.Variantes)
            {
                if (v.Id is { } vid && producto.Variantes.FirstOrDefault(x => x.Id == vid) is { } existente)
                {
                    existente.Sku = new Sku(v.Sku);
                    existente.CodigoBarras = string.IsNullOrWhiteSpace(v.CodigoBarras) ? null : new CodigoBarras(v.CodigoBarras);
                    existente.Talla = v.Talla;
                    existente.Color = v.Color;
                    existente.PrecioVenta = new Dinero(v.Precio);
                    existente.Costo = new Dinero(v.Costo);
                    existente.StockMinimo = v.StockMinimo;
                    _variantes.Actualizar(existente);
                    // El stock no se cambia aquí; se gestiona con «Ajustar stock».
                }
                else
                {
                    var nueva = new VarianteProducto
                    {
                        ProductoId = producto.Id,
                        Sku = new Sku(v.Sku),
                        CodigoBarras = string.IsNullOrWhiteSpace(v.CodigoBarras) ? null : new CodigoBarras(v.CodigoBarras),
                        Talla = v.Talla,
                        Color = v.Color,
                        PrecioVenta = new Dinero(v.Precio),
                        Costo = new Dinero(v.Costo),
                        StockMinimo = v.StockMinimo
                    };
                    nueva.EstablecerStock(v.StockInicial);
                    await _variantes.AgregarAsync(nueva, ct);
                    if (v.StockInicial != 0)
                        await _movimientos.AgregarAsync(new MovimientoInventario
                        {
                            VarianteId = nueva.Id,
                            Tipo = TipoMovimientoInventario.InventarioInicial,
                            Cantidad = v.StockInicial,
                            Motivo = "Variante agregada",
                            UsuarioId = usuarioId,
                            Fecha = _reloj.UtcAhora
                        }, ct);
                }
            }

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok();
    }

    public async Task<Result<Guid>> CrearAsync(NuevoProductoDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla<Guid>("El nombre del producto es obligatorio.");
        if (dto.Variantes.Count == 0)
            return Result.Falla<Guid>("El producto debe tener al menos una variante.");

        // Validar SKU únicos (dentro del lote y contra la base).
        var skusLote = dto.Variantes.Select(v => v.Sku.Trim().ToUpperInvariant()).ToList();
        if (skusLote.Distinct().Count() != skusLote.Count)
            return Result.Falla<Guid>("Hay SKU repetidos entre las variantes.");
        foreach (var sku in skusLote)
            if (await _variantes.ExisteSkuAsync(sku, null, ct))
                return Result.Falla<Guid>($"El SKU «{sku}» ya existe.");

        var producto = new Producto
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            Marca = dto.Marca,
            CategoriaId = dto.CategoriaId
        };

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            await _productos.AgregarAsync(producto, ct);

            foreach (var v in dto.Variantes)
            {
                var variante = new VarianteProducto
                {
                    ProductoId = producto.Id,
                    Sku = new Sku(v.Sku),
                    CodigoBarras = string.IsNullOrWhiteSpace(v.CodigoBarras) ? null : new CodigoBarras(v.CodigoBarras),
                    Talla = v.Talla,
                    Color = v.Color,
                    PrecioVenta = new Dinero(v.Precio),
                    Costo = new Dinero(v.Costo),
                    StockMinimo = v.StockMinimo
                };
                variante.EstablecerStock(v.StockInicial);
                await _variantes.AgregarAsync(variante, ct);

                if (v.StockInicial != 0)
                    await _movimientos.AgregarAsync(new MovimientoInventario
                    {
                        VarianteId = variante.Id,
                        Tipo = TipoMovimientoInventario.InventarioInicial,
                        Cantidad = v.StockInicial,
                        Motivo = "Alta de producto",
                        UsuarioId = usuarioId,
                        Fecha = _reloj.UtcAhora
                    }, ct);
            }

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok(producto.Id);
    }
}
