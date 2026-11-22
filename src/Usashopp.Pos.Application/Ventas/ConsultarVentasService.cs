using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Historial de ventas: consulta, detalle y reimpresión de ticket.</summary>
public class ConsultarVentasService
{
    private readonly IVentaRepository _ventas;
    private readonly IRepository<Usuario> _usuarios;
    private readonly ITicketPrinter _impresora;

    public ConsultarVentasService(IVentaRepository ventas, IRepository<Usuario> usuarios, ITicketPrinter impresora)
    {
        _ventas = ventas;
        _usuarios = usuarios;
        _impresora = impresora;
    }

    /// <summary>Una página del historial (más recientes primero) y el total de ventas del rango.</summary>
    public async Task<PaginaVentasDto> ListarAsync(
        DateTime? desde, DateTime? hasta, int saltar = 0, int tomar = 200, CancellationToken ct = default)
    {
        var desdeUtc = Fechas.LocalAUtc(desde);
        var hastaUtc = Fechas.LocalAUtc(hasta);
        var total = await _ventas.ContarPorFechaAsync(desdeUtc, hastaUtc, ct);
        var ventas = await _ventas.ListarPaginaPorFechaAsync(desdeUtc, hastaUtc, saltar, tomar, ct);
        var items = ventas.Select(v => new VentaResumenDto(
            v.Id, v.Folio, Fechas.UtcALocal(v.Fecha), v.Total.Monto,
            v.Detalles.Sum(d => d.Cantidad), Describir(v.Estado))).ToList();
        return new PaginaVentasDto(items, total);
    }

    /// <summary>La venta más reciente registrada (para "Última venta" en el POS).</summary>
    public async Task<VentaResumenDto?> ObtenerUltimaAsync(CancellationToken ct = default)
    {
        var ventas = await _ventas.ListarPaginaPorFechaAsync(null, null, 0, 1, ct);
        var v = ventas.FirstOrDefault();
        return v is null ? null : new VentaResumenDto(
            v.Id, v.Folio, Fechas.UtcALocal(v.Fecha), v.Total.Monto,
            v.Detalles.Sum(d => d.Cantidad), Describir(v.Estado));
    }

    /// <summary>Busca una venta por folio exacto (escaneo del ticket para cambios/devoluciones).</summary>
    public async Task<VentaResumenDto?> BuscarPorFolioAsync(string folio, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folio)) return null;
        var v = await _ventas.ObtenerPorFolioAsync(folio.Trim().ToUpperInvariant(), ct)
                ?? await _ventas.ObtenerPorFolioAsync(folio.Trim(), ct);
        return v is null ? null : new VentaResumenDto(
            v.Id, v.Folio, Fechas.UtcALocal(v.Fecha), v.Total.Monto,
            v.Detalles.Sum(d => d.Cantidad), Describir(v.Estado));
    }

    public async Task<VentaDetalleDto?> ObtenerDetalleAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _ventas.ObtenerConDetalleAsync(id, ct);
        if (v is null) return null;
        var usuario = (await _usuarios.ObtenerPorIdAsync(v.UsuarioId, ct))?.Nombre;
        return Mapear(v, usuario);
    }

    public async Task<Result> ReimprimirAsync(Guid id, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(id, ct);
        if (venta is null) return Result.Falla("La venta no existe.");
        try
        {
            await _impresora.ImprimirVentaAsync(venta, ct);
        }
        catch (Exception ex)
        {
            return Result.Falla($"No se pudo imprimir: {ex.Message}");
        }
        return Result.Ok();
    }

    private static string Describir(EstadoVenta estado) => estado switch
    {
        EstadoVenta.EnProceso => "En proceso",
        EstadoVenta.Pagada => "Pagada",
        EstadoVenta.Cancelada => "Cancelada",
        EstadoVenta.Devuelta => "Devuelta",
        EstadoVenta.ParcialmenteDevuelta => "Devuelta parcial",
        _ => estado.ToString()
    };

    private static VentaDetalleDto Mapear(Venta v, string? usuario) => new(
        v.Id, v.Folio, Fechas.UtcALocal(v.Fecha), v.Subtotal.Monto, v.Total.Monto, v.Cambio.Monto, Describir(v.Estado),
        v.Detalles.Select(d => new VentaLineaDetalleDto(
            d.Descripcion, d.Cantidad, d.PrecioUnitario.Monto, d.Importe.Monto,
            (d.PrecioUnitario.Monto * d.Cantidad) - d.Importe.Monto)).ToList(),
        v.Pagos.Select(p => new PagoResumenDto(EtiquetaPago(p.Metodo), p.Monto.Monto)).ToList(),
        v.Notas,
        v.TotalDescuentoGlobal.Monto,
        v.Cliente?.Nombre,
        usuario);

    internal static string EtiquetaPago(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Vales => "Vales",
        MetodoPago.Credito => "Crédito",
        MetodoPago.NotaCredito => "Nota de crédito",
        MetodoPago.CreditoCambio => "Crédito por cambio",
        _ => "Otro"
    };
}
