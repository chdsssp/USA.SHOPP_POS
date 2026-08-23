using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Productos;

/// <summary>
/// Búsqueda de productos para el POS. Primero intenta coincidencia exacta por código
/// de barras / SKU (uso con lector) y, si no, hace búsqueda por texto libre.
/// </summary>
public class BuscarProductosService
{
    private readonly IVarianteRepository _variantes;

    public BuscarProductosService(IVarianteRepository variantes) => _variantes = variantes;

    public async Task<ProductoBusquedaDto?> PorCodigoBarrasAsync(string codigo, CancellationToken ct = default)
    {
        var variante = await _variantes.ObtenerPorCodigoBarrasAsync(codigo, ct);
        return variante is null ? null : Mapear(variante);
    }

    public async Task<IReadOnlyList<ProductoBusquedaDto>> PorTextoAsync(string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return Array.Empty<ProductoBusquedaDto>();

        var variantes = await _variantes.BuscarAsync(texto.Trim(), 50, ct);
        return variantes.Select(Mapear).ToList();
    }

    /// <summary>Listado para el grid táctil, opcionalmente filtrado por categoría.</summary>
    public async Task<IReadOnlyList<ProductoBusquedaDto>> ParaGridAsync(Guid? categoriaId = null, CancellationToken ct = default)
    {
        var variantes = await _variantes.ListarParaVentaAsync(categoriaId, 200, ct);
        return variantes.Select(Mapear).ToList();
    }

    private static ProductoBusquedaDto Mapear(VarianteProducto v) => new(
        v.Id,
        v.DescripcionCompleta,
        v.Sku.Valor,
        v.CodigoBarras?.Valor,
        v.PrecioVenta.Monto,
        v.StockActual,
        v.EstaBajoMinimo);
}
