using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Compras;

public class ConsultarComprasService
{
    private readonly ICompraRepository _compras;
    private readonly IRepository<PagoCompra> _pagos;

    public ConsultarComprasService(ICompraRepository compras, IRepository<PagoCompra> pagos)
    {
        _compras = compras;
        _pagos = pagos;
    }

    public async Task<IReadOnlyList<CompraResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var compras = await _compras.ListarAsync(ct);
        var pagos = await _pagos.ListarAsync(null, ct);
        var pagadoPorCompra = pagos
            .GroupBy(p => p.CompraId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Monto.Monto));

        return compras.Select(c =>
        {
            var total = c.Total.Monto;
            var pagado = pagadoPorCompra.GetValueOrDefault(c.Id, 0m);
            return new CompraResumenDto(
                c.Id, c.Folio, c.Proveedor?.Nombre ?? "—", c.Fecha, total, c.Estado.ToString(),
                pagado, total - pagado);
        }).ToList();
    }

    public async Task<CompraDetalleDto?> ObtenerDetalleAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _compras.ObtenerConDetalleAsync(id, ct);
        if (c is null) return null;
        return new CompraDetalleDto(
            c.Id, c.Folio, c.Proveedor?.Nombre ?? "—", c.Fecha, c.Total.Monto, c.Estado.ToString(),
            c.Detalles.Select(d => new CompraLineaDetalleDto(
                d.Variante?.DescripcionCompleta ?? "Producto", d.Cantidad, d.CostoUnitario.Monto, d.Importe.Monto,
                d.Id, d.VarianteId, d.CantidadRecibida, d.Pendiente)).ToList());
    }
}
