using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Application.Common.Models;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Actualizador compatible con el instalador de Inno Setup que ya usa la tienda. Funciona sin
/// internet: basta copiar el nuevo USASHOPP-POS-Setup-X.Y.Z.exe a una USB o carpeta compartida.
/// Antes de instalar crea un respaldo verificado de la base.
/// </summary>
public partial class ActualizadorInstalador : IActualizador
{
    private readonly IBackupService _backup;

    public ActualizadorInstalador(IBackupService backup) => _backup = backup;

    [GeneratedRegex(@"USASHOPP-POS-Setup-(\d+\.\d+\.\d+)\.exe$", RegexOptions.IgnoreCase)]
    private static partial Regex PatronInstalador();

    public Version VersionActual
    {
        get
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var limpia = info?.Split('+', '-')[0];
            return Version.TryParse(limpia, out var v) ? v : asm.GetName().Version ?? new Version(0, 0, 0);
        }
    }

    public ActualizacionDisponible? Buscar(string? carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta)) return null;
        try
        {
            if (!Directory.Exists(carpeta)) return null;
            return Directory.EnumerateFiles(carpeta, "USASHOPP-POS-Setup-*.exe", SearchOption.TopDirectoryOnly)
                .Select(f => (Ruta: f, M: PatronInstalador().Match(Path.GetFileName(f))))
                .Where(x => x.M.Success && Version.TryParse(x.M.Groups[1].Value, out _))
                .Select(x => new ActualizacionDisponible(Version.Parse(x.M.Groups[1].Value), x.Ruta))
                .Where(a => Comparar(a.Version, VersionActual) > 0)
                .OrderByDescending(a => a.Version)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo revisar la carpeta de actualizaciones {Carpeta}", carpeta);
            return null;
        }
    }

    public async Task<Result> IniciarInstalacionAsync(ActualizacionDisponible a, CancellationToken ct = default)
    {
        if (!File.Exists(a.RutaInstalador)) return Result.Falla("El instalador ya no está en la carpeta (¿se retiró la USB?).");
        try
        {
            var respaldo = await _backup.CrearRespaldoAsync(OrigenRespaldo.PreActualizacion, a.VersionTexto, ct);
            if (!_backup.VerificarRespaldo(respaldo))
                return Result.Falla("El respaldo previo a la actualización no pasó la verificación; no se actualizó.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló el respaldo previo a la actualización");
            return Result.Falla("No se pudo respaldar la base antes de actualizar: " + ex.Message);
        }

        // Se copia el instalador a una carpeta temporal: la USB se puede retirar durante la instalación.
        var local = Path.Combine(Path.GetTempPath(), Path.GetFileName(a.RutaInstalador));
        File.Copy(a.RutaInstalador, local, overwrite: true);

        Log.Information("Iniciando actualización a {Version} desde {Ruta}", a.VersionTexto, a.RutaInstalador);
        Process.Start(new ProcessStartInfo(local, "/SILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /NORESTART")
        {
            UseShellExecute = true, // pide permisos de administrador (UAC) si hacen falta
            Verb = "open"
        });
        return Result.Ok();
    }

    private static int Comparar(Version a, Version b) =>
        new Version(a.Major, a.Minor, Math.Max(0, a.Build)).CompareTo(new Version(b.Major, b.Minor, Math.Max(0, b.Build)));
}
