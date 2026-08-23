using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Infrastructure.Persistence;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Inicio de sesión TEMPORAL como administrador, hasta que exista la pantalla de login
/// (Fase 6). Permite operar caja y ventas durante el desarrollo de las fases 3–5.
/// </summary>
public static class SesionBootstrap
{
    public static async Task IniciarComoAdminAsync(IServiceProvider scoped, CancellationToken ct = default)
    {
        var db = scoped.GetRequiredService<AppDbContext>();
        var admin = await db.Usuarios
            .Include(u => u.Rol).ThenInclude(r => r!.Permisos)
            .FirstOrDefaultAsync(u => u.UsuarioLogin == "admin", ct);

        if (admin?.Rol is null) return;

        var currentUser = scoped.GetRequiredService<CurrentUserService>();
        currentUser.IniciarSesion(admin.Id, admin.Nombre, admin.Rol.Permisos.Select(p => p.Clave));
    }
}
