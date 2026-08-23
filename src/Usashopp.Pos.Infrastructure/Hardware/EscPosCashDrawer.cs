using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Apertura del cajón de dinero.
///
/// TODO (Fase 4 — Hardware): enviar el comando ESC/POS "drawer kick" (ESC p m t1 t2) a
/// la impresora de tickets a la que está conectado el cajón.
/// </summary>
public class EscPosCashDrawer : ICashDrawer
{
    public Task AbrirAsync(CancellationToken ct = default)
    {
        Log.Information("Apertura de cajón solicitada (pendiente de drawer-kick ESC/POS).");
        return Task.CompletedTask;
    }
}
