using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Historial de ventas: consulta, detalle y reimpresión de ticket.</summary>
public class ConsultarVentasService
{
    private readonly IVentaRepository _ventas;
    private readonly ITicketPrinter _impresora;

    public ConsultarVentasService(IVentaRepository ventas, ITicketPrinter impresora)
    {
        _ventas = ventas;
        _impresora = impresora;
    }

    public async Task<IReadOnlyList<VentaResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        var ventas = await _ventas.ListarPorFechaAsync(desde, hasta, ct);
        return ventas.Select(v => new VentaResumenDto(
            v.Id, v.Folio, v.Fecha, v.Total.Monto,
            v.Detalles.Sum(d => d.Cantidad), v.Estado.ToString())).ToList();
    }

    public async Task<VentaDetalleDto?> ObtenerDetalleAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _ventas.ObtenerConDetalleAsync(id, ct);
        return v is null ? null : Mapear(v);
    }

    public async Task<Result> ReimprimirAsync(Guid id, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(id, ct);
        if (venta is null) return Result.Falla("La venta no existe.");
        await _impresora.ImprimirVentaAsync(venta, ct);
        return Result.Ok();
    }

    private static VentaDetalleDto Mapear(Venta v) => new(
        v.Id, v.Folio, v.Fecha, v.Subtotal.Monto, v.Total.Monto, v.Cambio.Monto, v.Estado.ToString(),
        v.Detalles.Select(d => new VentaLineaDetalleDto(d.Descripcion, d.Cantidad, d.PrecioUnitario.Monto, d.Importe.Monto)).ToList(),
        v.Pagos.Select(p => new PagoResumenDto(p.Metodo.ToString(), p.Monto.Monto)).ToList(),
        v.Notas);
}
