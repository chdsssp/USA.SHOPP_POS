using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Impresión de tickets vía ESC/POS.
///
/// TODO (Fase 4 — Hardware): construir la secuencia de comandos ESC/POS (encabezado,
/// líneas, totales en tamaño doble, corte de papel) y enviarla a la impresora térmica
/// configurada (por nombre de impresora de Windows o vía librería como ESCPOS_NET).
/// Por ahora deja traza en el log para poder integrar el flujo completo sin hardware.
/// </summary>
public class EscPosTicketPrinter : ITicketPrinter
{
    public Task ImprimirVentaAsync(Venta venta, CancellationToken ct = default)
    {
        Log.Information("Ticket (pendiente de ESC/POS) → Folio {Folio}, Total {Total}, {Lineas} líneas",
            venta.Folio, venta.Total, venta.Detalles.Count);
        return Task.CompletedTask;
    }

    public Task ImprimirPruebaAsync(CancellationToken ct = default)
    {
        Log.Information("Impresión de prueba solicitada (pendiente de ESC/POS).");
        return Task.CompletedTask;
    }
}
