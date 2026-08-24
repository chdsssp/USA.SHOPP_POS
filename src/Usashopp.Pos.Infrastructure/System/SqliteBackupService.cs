using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Infrastructure.Persistence;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Respaldo de la base de datos SQLite: hace checkpoint del WAL y copia el archivo .db
/// con marca de tiempo. Opcionalmente copia también a una carpeta en la nube.
/// </summary>
public class SqliteBackupService : IBackupService
{
    private readonly AppDbContext _db;
    private readonly InfrastructureOptions _opciones;

    public SqliteBackupService(AppDbContext db, IOptions<InfrastructureOptions> opciones)
    {
        _db = db;
        _opciones = opciones.Value;
    }

    public async Task<string> CrearRespaldoAsync(CancellationToken ct = default)
    {
        // Consolida el WAL para que el archivo .db esté completo.
        await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", ct);

        var rutaDb = ObtenerRutaBaseDatos();
        Directory.CreateDirectory(_opciones.CarpetaRespaldos);

        var nombre = $"pos_{DateTime.Now:yyyyMMdd_HHmm}.db";
        var destino = Path.Combine(_opciones.CarpetaRespaldos, nombre);
        File.Copy(rutaDb, destino, overwrite: true);

        if (!string.IsNullOrWhiteSpace(_opciones.CarpetaNube))
        {
            Directory.CreateDirectory(_opciones.CarpetaNube);
            File.Copy(destino, Path.Combine(_opciones.CarpetaNube, nombre), overwrite: true);
        }

        LimpiarAntiguos();
        Log.Information("Respaldo creado en {Destino}", destino);
        return destino;
    }

    public Task RestaurarAsync(string rutaRespaldo, CancellationToken ct = default)
    {
        if (!File.Exists(rutaRespaldo))
            throw new FileNotFoundException("No se encontró el archivo de respaldo.", rutaRespaldo);

        var rutaDb = ObtenerRutaBaseDatos();
        CopiarSobreBaseDatos(rutaRespaldo, rutaDb);
        Log.Warning("Base de datos restaurada desde {Origen}", rutaRespaldo);
        return Task.CompletedTask;
    }

    private string RutaMarcadorRestauracion =>
        Path.Combine(_opciones.CarpetaRespaldos, "restore.pending");

    public void ProgramarRestauracion(string rutaRespaldo)
    {
        if (!File.Exists(rutaRespaldo))
            throw new FileNotFoundException("No se encontró el archivo de respaldo.", rutaRespaldo);

        Directory.CreateDirectory(_opciones.CarpetaRespaldos);
        File.WriteAllText(RutaMarcadorRestauracion, rutaRespaldo);
        Log.Information("Restauración programada desde {Origen} (se aplicará al reiniciar).", rutaRespaldo);
    }

    public bool AplicarRestauracionPendiente()
    {
        var marcador = RutaMarcadorRestauracion;
        if (!File.Exists(marcador)) return false;

        try
        {
            var origen = File.ReadAllText(marcador).Trim();
            if (!string.IsNullOrWhiteSpace(origen) && File.Exists(origen))
            {
                var rutaDb = ObtenerRutaBaseDatos();
                CopiarSobreBaseDatos(origen, rutaDb);
                Log.Warning("Base de datos restaurada al arranque desde {Origen}", origen);
            }
            else
            {
                Log.Warning("El respaldo a restaurar ya no existe: {Origen}", origen);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Falló la restauración pendiente");
        }
        finally
        {
            try { File.Delete(marcador); } catch (IOException) { /* se reintenta luego */ }
        }
        return true;
    }

    /// <summary>Sobrescribe el .db con el respaldo y elimina los archivos WAL/SHM huérfanos.</summary>
    private static void CopiarSobreBaseDatos(string origen, string rutaDb)
    {
        File.Copy(origen, rutaDb, overwrite: true);
        // El respaldo ya trae el WAL consolidado; eliminamos WAL/SHM previos para no mezclar estados.
        foreach (var sufijo in new[] { "-wal", "-shm" })
        {
            var ruta = rutaDb + sufijo;
            if (File.Exists(ruta))
                try { File.Delete(ruta); } catch (IOException) { /* se limpia en el próximo arranque */ }
        }
    }

    private string ObtenerRutaBaseDatos()
    {
        var conexion = _db.Database.GetDbConnection();
        // En SQLite, DataSource es la ruta del archivo .db.
        return conexion.DataSource;
    }

    private void LimpiarAntiguos()
    {
        var archivos = Directory.GetFiles(_opciones.CarpetaRespaldos, "pos_*.db")
            .OrderByDescending(f => f)
            .Skip(_opciones.RetenerUltimos);

        foreach (var archivo in archivos)
        {
            try { File.Delete(archivo); }
            catch (IOException) { /* se reintenta en el próximo respaldo */ }
        }
    }
}
