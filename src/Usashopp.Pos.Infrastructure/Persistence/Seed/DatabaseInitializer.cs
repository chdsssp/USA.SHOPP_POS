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

        await _db.Roles.AddRangeAsync(admin, encargado, cajero, ct);

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

    private async Task SembrarConfiguracionAsync(CancellationToken ct)
    {
        if (await _db.Configuracion.AnyAsync(ct)) return;
        await _db.Configuracion.AddAsync(new ConfiguracionTienda(), ct);
    }
}
