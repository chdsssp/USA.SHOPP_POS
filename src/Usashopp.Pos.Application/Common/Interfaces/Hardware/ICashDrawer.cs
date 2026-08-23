namespace Usashopp.Pos.Application.Common.Interfaces.Hardware;

/// <summary>
/// Cajón de dinero. Normalmente se abre enviando el comando "drawer kick" a la
/// impresora de tickets a la que está conectado.
/// </summary>
public interface ICashDrawer
{
    Task AbrirAsync(CancellationToken cancellationToken = default);
}
