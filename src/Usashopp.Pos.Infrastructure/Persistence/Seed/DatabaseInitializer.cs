using Microsoft.EntityFrameworkCore;
using Serilog;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Infrastructure.Persistence.Seed;

/// <summary>
/// Aplica migraciones al arrancar y siembra los datos iniciales (permisos, roles,
/// usuario administrador y configuración de tienda).
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IBackupService _backup;

    public DatabaseInitializer(AppDbContext db, IPasswordHasher hasher, IBackupService backup)
    {
        _db = db;
        _hasher = hasher;
        _backup = backup;
    }

    public async Task InicializarAsync(CancellationToken ct = default)
    {
        await MigrarConRespaldoAsync(ct);

        // Robustez y rendimiento de SQLite.
        await _db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
        await _db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", ct);

        await SembrarPermisosYRolesAsync(ct);
        await SincronizarPermisosAsync(ct);
        await SembrarConfiguracionAsync(ct);
        await SembrarCategoriaBaseAsync(ct);

        await _db.SaveChangesAsync(ct);
        Log.Information("Base de datos inicializada.");
    }

    /// <summary>
    /// Si hay migraciones pendientes sobre una base con datos, primero crea un respaldo verificado
    /// ("antes de actualizar BD"). Sin respaldo no se migra: una migración mala sobre datos reales
    /// es cara de revertir. Si la migración falla, se informa dónde quedó el respaldo para restaurar.
    /// </summary>
    private async Task MigrarConRespaldoAsync(CancellationToken ct)
    {
        var pendientes = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pendientes.Count == 0) return;

        var aplicadas = (await _db.Database.GetAppliedMigrationsAsync(ct)).ToList();
        string? respaldo = null;
        if (aplicadas.Count > 0)
        {
            try
            {
                var etiqueta = pendientes[^1].Split('_').Last();
                respaldo = await _backup.CrearRespaldoAsync(OrigenRespaldo.PreMigracion, etiqueta, ct);
                if (!_backup.VerificarRespaldo(respaldo))
                    throw new InvalidOperationException("El respaldo previo a la actualización no pasó la verificación.");
                Log.Information("Respaldo previo a migración ({Pendientes}) en {Ruta}", string.Join(", ", pendientes), respaldo);
            }
            catch (Exception ex)
            {
                throw new ErrorInicializacionException(
                    "No se pudo respaldar la base de datos antes de actualizarla, así que no se actualizó. " +
                    "Revisa el espacio en disco y los permisos de la carpeta de respaldos.", null, ex);
            }
        }

        try
        {
            await _db.Database.MigrateAsync(ct);
        }
        catch (Exception ex)
        {
            throw new ErrorInicializacionException(
                "Falló la actualización de la base de datos (" + string.Join(", ", pendientes) + ").", respaldo, ex);
        }
    }

    private async Task SembrarCategoriaBaseAsync(CancellationToken ct)
    {
        if (await _db.Categorias.AnyAsync(ct)) return;
        await _db.Categorias.AddAsync(new Categoria { Nombre = "General" }, ct);
    }

    private async Task SembrarPermisosYRolesAsync(CancellationToken ct)
    {
        if (await _db.Roles.AnyAsync(ct)) return;

        // Permisos
        var permisos = Permisos.Todos.ToDictionary(
            clave => clave,
            clave => new Permiso { Clave = clave });
        await _db.Permisos.AddRangeAsync(permisos.Values, ct);

        // Roles
        var admin = new Rol { Nombre = "Administrador" };
        foreach (var p in permisos.Values) admin.Permisos.Add(p);

        var encargado = new Rol { Nombre = "Encargado" };
        foreach (var clave in Permisos.Encargado) encargado.Permisos.Add(permisos[clave]);

        var cajero = new Rol { Nombre = "Cajero" };
        foreach (var clave in Permisos.Cajero) cajero.Permisos.Add(permisos[clave]);

        await _db.Roles.AddRangeAsync(new[] { admin, encargado, cajero }, ct);

        // Usuario administrador inicial (debe cambiarse en el primer inicio de sesión).
        var usuarioAdmin = new Usuario
        {
            Nombre = "Administrador",
            UsuarioLogin = "admin",
            HashContrasena = _hasher.Hash("admin"),
            Rol = admin
        };
        await _db.Usuarios.AddAsync(usuarioAdmin, ct);
    }

    /// <summary>
    /// Da de alta los permisos del catálogo que aún no existan (para bases ya creadas al
    /// agregar permisos nuevos en versiones posteriores) y se los asigna al rol Administrador,
    /// que siempre debe tenerlos todos. Idempotente: se puede ejecutar en cada arranque.
    /// </summary>
    private async Task SincronizarPermisosAsync(CancellationToken ct)
    {
        // Considera tanto los permisos ya persistidos como los recién sembrados en esta misma
        // operación (aún sin guardar). En una BD nueva, SembrarPermisosYRolesAsync ya los agregó
        // y una consulta a la BD no los vería, provocando duplicados por clave.
        await _db.Permisos.ToListAsync(ct); // asegura que los persistidos estén rastreados
        var existentes = _db.Permisos.Local.ToList();
        var claves = existentes.Select(p => p.Clave).ToHashSet();

        var nuevos = Permisos.Todos
            .Where(clave => !claves.Contains(clave))
            .Select(clave => new Permiso { Clave = clave })
            .ToList();

        if (nuevos.Count > 0)
        {
            await _db.Permisos.AddRangeAsync(nuevos, ct);
            existentes.AddRange(nuevos);
        }

        // El rol Administrador debe tener todos los permisos (si ya existe en la BD).
        var admin = await _db.Roles
            .Include(r => r.Permisos)
            .FirstOrDefaultAsync(r => r.Nombre == "Administrador", ct);
        if (admin is not null)
        {
            var delAdmin = admin.Permisos.Select(p => p.Clave).ToHashSet();
            foreach (var p in existentes.Where(p => !delAdmin.Contains(p.Clave)))
                admin.Permisos.Add(p);
        }
    }

    private async Task SembrarConfiguracionAsync(CancellationToken ct)
    {
        if (await _db.Configuracion.AnyAsync(ct)) return;
        await _db.Configuracion.AddAsync(new ConfiguracionTienda(), ct);
    }
}
