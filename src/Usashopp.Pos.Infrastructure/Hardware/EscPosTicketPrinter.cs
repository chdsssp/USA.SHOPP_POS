using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Impresión de tickets vía ESC/POS enviando bytes RAW a la impresora de Windows configurada
/// (papel de 80 mm). Si no hay impresora configurada, no imprime (la venta ya quedó registrada
/// y existe la vista previa en pantalla).
/// </summary>
public class EscPosTicketPrinter : ITicketPrinter
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EscPosTicketPrinter(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task ImprimirVentaAsync(Venta venta, CancellationToken ct = default)
    {
        var c = await ObtenerConfigAsync(ct);
        if (string.IsNullOrWhiteSpace(c.ImpresoraTicket))
        {
            Log.Information("Ticket no impreso: no hay impresora configurada (Folio {Folio}).", venta.Folio);
            return;
        }

        var bytes = EscPosTicket.Venta(
            venta, c.NombreTienda, c.Direccion, c.Telefono, c.Rfc, c.MensajePieTicket);
        await Task.Run(() => RawPrinterHelper.EnviarBytes(c.ImpresoraTicket!, bytes), ct);
        Log.Information("Ticket impreso en «{Impresora}» (Folio {Folio}).", c.ImpresoraTicket, venta.Folio);
    }

    public async Task ImprimirPruebaAsync(CancellationToken ct = default)
    {
        var c = await ObtenerConfigAsync(ct);
        if (string.IsNullOrWhiteSpace(c.ImpresoraTicket))
            throw new InvalidOperationException("No hay una impresora configurada. Selecciona una y guarda antes de imprimir la prueba.");

        var bytes = EscPosTicket.Prueba(c.NombreTienda);
        await Task.Run(() => RawPrinterHelper.EnviarBytes(c.ImpresoraTicket!, bytes), ct);
    }

    private async Task<ConfiguracionTienda> ObtenerConfigAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfiguracionTiendaRepository>();
        return await repo.ObtenerAsync(ct);
    }
}
