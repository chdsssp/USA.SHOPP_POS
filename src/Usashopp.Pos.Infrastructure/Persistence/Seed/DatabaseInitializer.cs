using Microsoft.EntityFrameworkCore;
using Serilog;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
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

    public DatabaseInitializer(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task InicializarAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);

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
        var existentes = await _db.Permisos.ToListAsync(ct);
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

        // El rol Administrador debe tener todos los permisos.
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
