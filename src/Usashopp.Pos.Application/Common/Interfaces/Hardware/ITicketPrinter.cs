using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces.Hardware;

/// <summary>
/// Impresión de tickets en impresora térmica (ESC/POS). La implementación concreta
/// (comandos ESC/POS, corte de papel) vive en Infrastructure.
/// </summary>
public interface ITicketPrinter
{
    Task ImprimirVentaAsync(Venta venta, CancellationToken cancellationToken = default);

    Task ImprimirPruebaAsync(CancellationToken cancellationToken = default);
}
