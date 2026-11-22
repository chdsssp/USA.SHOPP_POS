using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Infrastructure.Persistence;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Respaldo de la base de datos SQLite con la API de respaldo en línea (copia consistente aunque
/// la app esté escribiendo), verificación de integridad, copia a carpeta externa (USB/disco) y a
/// la nube, y retención escalonada (diarios / semanales / mensuales).
/// </summary>
public class SqliteBackupService : IBackupService
{
    private const string Prefijo = "pos_";
    private const string ExtVerificado = ".ok";
    private const string ExtDanado = ".bad";

    private readonly AppDbContext _db;
    private readonly InfrastructureOptions _opciones;
    private readonly IPreferenciasRespaldo _preferencias;

    public SqliteBackupService(AppDbContext db, IOptions<InfrastructureOptions> opciones, IPreferenciasRespaldo preferencias)
    {
        _db = db;
        _opciones = opciones.Value;
        _preferencias = preferencias;
    }

    public Task<string> CrearRespaldoAsync(CancellationToken ct = default) =>
        CrearRespaldoAsync(OrigenRespaldo.Manual, null, ct);

    public async Task<string> CrearRespaldoAsync(OrigenRespaldo origen, string? etiqueta = null, CancellationToken ct = default)
    {
        var rutaDb = ObtenerRutaBaseDatos();
        Directory.CreateDirectory(_opciones.CarpetaRespaldos);

        var sufijo = string.IsNullOrWhiteSpace(etiqueta) ? "" : "_" + Sanitizar(etiqueta);
        var nombre = $"{Prefijo}{DateTime.Now:yyyyMMdd_HHmmss}_{origen.ToString().ToLowerInvariant()}{sufijo}.db";
        var destino = Path.Combine(_opciones.CarpetaRespaldos, nombre);

        await Task.Run(() => CopiarEnLinea(rutaDb, destino), ct);

        var integro = VerificarRespaldo(destino);
        File.WriteAllText(destino + (integro ? ExtVerificado : ExtDanado), DateTime.Now.ToString("O"));
        if (!integro)
            Log.Error("El respaldo {Destino} NO pasó la verificación de integridad", destino);

        CopiarADestinoAdicional(destino, nombre, _preferencias.CarpetaRespaldoExterno, "externa");
        CopiarADestinoAdicional(destino, nombre, _opciones.CarpetaNube, "nube");

        LimpiarAntiguos();
        Log.Information("Respaldo {Origen} creado en {Destino} (íntegro: {Integro})", origen, destino, integro);
        return destino;
    }

    /// <summary>Copia consistente con sqlite3_backup (no un File.Copy del archivo en uso).</summary>
    private static void CopiarEnLinea(string rutaDb, string destino)
    {
        using (var origen = new SqliteConnection($"Data Source={rutaDb};Mode=ReadOnly;Pooling=False"))
        using (var copia = new SqliteConnection($"Data Source={destino};Pooling=False"))
        {
            origen.Open();
            copia.Open();
            origen.BackupDatabase(copia);
            // El respaldo queda en modo DELETE (un solo archivo, sin -wal) para poder moverlo/copiarlo.
            using var cmd = copia.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=DELETE;";
            cmd.ExecuteNonQuery();
        }
    }

    public bool VerificarRespaldo(string ruta)
    {
        try
        {
            using var c = new SqliteConnection($"Data Source={ruta};Mode=ReadOnly;Pooling=False");
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var r = cmd.ExecuteScalar() as string;
            cmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
            var migraciones = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            return string.Equals(r, "ok", StringComparison.OrdinalIgnoreCase) && migraciones > 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo verificar el respaldo {Ruta}", ruta);
            return false;
        }
    }

    private static void CopiarADestinoAdicional(string archivo, string nombre, string? carpeta, string que)
    {
        if (string.IsNullOrWhiteSpace(carpeta)) return;
        try
        {
            var raiz = Path.GetPathRoot(carpeta);
            if (!string.IsNullOrEmpty(raiz) && !Directory.Exists(raiz))
            {
                Log.Warning("Carpeta {Que} {Carpeta} no disponible (¿USB desconectada?): respaldo pendiente", que, carpeta);
                return;
            }
            Directory.CreateDirectory(carpeta);
            File.Copy(archivo, Path.Combine(carpeta, nombre), overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo copiar el respaldo a la carpeta {Que} {Carpeta}", que, carpeta);
        }
    }

    public IReadOnlyList<RespaldoInfo> ListarRespaldos()
    {
        if (!Directory.Exists(_opciones.CarpetaRespaldos)) return Array.Empty<RespaldoInfo>();
        return Directory.GetFiles(_opciones.CarpetaRespaldos, Prefijo + "*.db")
            .Select(f => new FileInfo(f))
            .Select(fi => new RespaldoInfo(
                fi.FullName, fi.Name, fi.LastWriteTime, fi.Length, InferirOrigen(fi.Name),
                File.Exists(fi.FullName + ExtVerificado) ? true : File.Exists(fi.FullName + ExtDanado) ? false : null))
            .OrderByDescending(r => r.Fecha)
            .ToList();
    }

    public EstadoRespaldoExterno ObtenerEstadoExterno()
    {
        var carpeta = _preferencias.CarpetaRespaldoExterno;
        if (string.IsNullOrWhiteSpace(carpeta)) return new EstadoRespaldoExterno(false, false, null);
        try
        {
            if (!Directory.Exists(carpeta)) return new EstadoRespaldoExterno(true, false, null);
            var ultimo = Directory.GetFiles(carpeta, Prefijo + "*.db")
                .Select(f => (DateTime?)File.GetLastWriteTime(f))
                .DefaultIfEmpty(null)
                .Max();
            return new EstadoRespaldoExterno(true, true, ultimo);
        }
        catch
        {
            return new EstadoRespaldoExterno(true, false, null);
        }
    }

    internal static OrigenRespaldo InferirOrigen(string nombre)
    {
        foreach (var o in Enum.GetValues<OrigenRespaldo>())
            if (nombre.Contains("_" + o.ToString().ToLowerInvariant(), StringComparison.Ordinal))
                return o;
        return OrigenRespaldo.Manual; // respaldos anteriores (pos_yyyyMMdd_HHmm.db)
    }

    private static string Sanitizar(string texto) =>
        new(texto.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());

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
                // Red de seguridad: guarda la base actual antes de reemplazarla (la base aún no está abierta).
                if (File.Exists(rutaDb))
                {
                    Directory.CreateDirectory(_opciones.CarpetaRespaldos);
                    var previa = Path.Combine(_opciones.CarpetaRespaldos,
                        $"{Prefijo}{DateTime.Now:yyyyMMdd_HHmmss}_{OrigenRespaldo.Manual.ToString().ToLowerInvariant()}_antes-de-restaurar.db");
                    try { CopiarEnLinea(rutaDb, previa); }
                    catch (Exception ex) { Log.Warning(ex, "No se pudo respaldar la base antes de restaurar"); }
                }
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
        var todos = ListarRespaldos().Select(r => (r.Ruta, r.Fecha, r.Origen)).ToList();
        foreach (var ruta in SeleccionarParaBorrar(todos, DateTime.Now, _opciones.RetenerUltimos))
        {
            try
            {
                File.Delete(ruta);
                foreach (var ext in new[] { ExtVerificado, ExtDanado })
                    if (File.Exists(ruta + ext)) File.Delete(ruta + ext);
            }
            catch (IOException) { /* se reintenta en el próximo respaldo */ }
        }
    }

    /// <summary>
    /// Retención escalonada: todo lo de los últimos 14 días (y al menos los
    /// <paramref name="minimoRecientes"/> más recientes), luego el más reciente de cada semana por
    /// 8 semanas y de cada mes por 12 meses. Los 5 respaldos previos a migración se conservan siempre.
    /// </summary>
    internal static IEnumerable<string> SeleccionarParaBorrar(
        IReadOnlyList<(string Ruta, DateTime Fecha, OrigenRespaldo Origen)> respaldos, DateTime ahora, int minimoRecientes = 30)
    {
        var conservar = new HashSet<string>();
        var ordenados = respaldos.OrderByDescending(r => r.Fecha).ToList();

        foreach (var r in ordenados.Where(r => r.Origen == OrigenRespaldo.PreMigracion).Take(5)) conservar.Add(r.Ruta);
        foreach (var r in ordenados.Where(r => r.Origen != OrigenRespaldo.PreMigracion).Take(minimoRecientes)) conservar.Add(r.Ruta);
        foreach (var r in ordenados.Where(r => r.Fecha >= ahora.AddDays(-14))) conservar.Add(r.Ruta);

        foreach (var g in ordenados.Where(r => r.Fecha >= ahora.AddDays(-7 * 8))
                     .GroupBy(r => ISOWeek.GetYear(r.Fecha) * 100 + ISOWeek.GetWeekOfYear(r.Fecha)))
            conservar.Add(g.First().Ruta);

        foreach (var g in ordenados.Where(r => r.Fecha >= ahora.AddMonths(-12))
                     .GroupBy(r => r.Fecha.Year * 100 + r.Fecha.Month))
            conservar.Add(g.First().Ruta);

        return ordenados.Where(r => !conservar.Contains(r.Ruta)).Select(r => r.Ruta).ToList();
    }
}
