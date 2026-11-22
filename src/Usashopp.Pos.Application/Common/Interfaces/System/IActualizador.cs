namespace Usashopp.Pos.Application.Common.Interfaces.System;

/// <summary>Instalador más nuevo encontrado en la carpeta de actualizaciones (USB o carpeta compartida).</summary>
public record ActualizacionDisponible(Version Version, string RutaInstalador)
{
    public string VersionTexto => Version.ToString(3);
}

/// <summary>
/// Actualización SIN internet: busca un instalador (USASHOPP-POS-Setup-X.Y.Z.exe) más nuevo que la
/// versión actual en una carpeta (USB o red local), respalda la base y lo ejecuta en modo silencioso.
/// </summary>
public interface IActualizador
{
    Version VersionActual { get; }

    ActualizacionDisponible? Buscar(string? carpeta);

    /// <summary>Respalda la base ("antes de actualizar app") y lanza el instalador. La app debe cerrarse después.</summary>
    Task<Models.Result> IniciarInstalacionAsync(ActualizacionDisponible actualizacion, CancellationToken cancellationToken = default);
}
