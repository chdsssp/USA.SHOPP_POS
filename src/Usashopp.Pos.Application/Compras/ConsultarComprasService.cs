using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Compras;

public class ConsultarComprasService
{
    private readonly ICompraRepository _compras;

    public ConsultarComprasService(ICompraRepository compras) => _compras = compras;

    public async Task<IReadOnlyList<CompraResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var compras = await _compras.ListarAsync(ct);
        return compras.Select(c => new CompraResumenDto(
            c.Id, c.Folio, c.Proveedor?.Nombre ?? "—", c.Fecha, c.Total.Monto, c.Estado.ToString())).ToList();
    }

    public async Task<CompraDetalleDto?> ObtenerDetalleAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _compras.ObtenerConDetalleAsync(id, ct);
        if (c is null) return null;
        return new CompraDetalleDto(
            c.Id, c.Folio, c.Proveedor?.Nombre ?? "—", c.Fecha, c.Total.Monto, c.Estado.ToString(),
            c.Detalles.Select(d => new CompraLineaDetalleDto(
                d.Variante?.DescripcionCompleta ?? "Producto", d.Cantidad, d.CostoUnitario.Monto, d.Importe.Monto)).ToList());
    }
}
