using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Apertura del cajón de dinero enviando el pulso ESC/POS "drawer-kick" a la impresora de
/// tickets configurada (a la que está conectado el cajón). Sin impresora configurada, no hace nada.
/// </summary>
public class EscPosCashDrawer : ICashDrawer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EscPosCashDrawer(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task AbrirAsync(CancellationToken ct = default)
    {
        string? impresora;
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IConfiguracionTiendaRepository>();
            impresora = (await repo.ObtenerAsync(ct)).ImpresoraTicket;
        }

        if (string.IsNullOrWhiteSpace(impresora))
        {
            Log.Information("Cajón no abierto: no hay impresora configurada.");
            return;
        }

        await Task.Run(() => RawPrinterHelper.EnviarBytes(impresora!, EscPosTicket.AbrirCajon), ct);
    }
}
