using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Usuarios;

/// <summary>Valida credenciales de acceso.</summary>
public class AutenticacionService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IRepository<RegistroAuditoria> _auditoria;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public AutenticacionService(
        IUsuarioRepository usuarios,
        IPasswordHasher hasher,
        IRepository<RegistroAuditoria> auditoria,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _auditoria = auditoria;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task<Result<SesionUsuarioDto>> ValidarAsync(string login, string contrasena, CancellationToken ct = default)
    {
        const string generico = "Usuario o contraseña incorrectos.";

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(contrasena))
            return Result.Falla<SesionUsuarioDto>(generico);

        var usuario = await _usuarios.ObtenerPorLoginAsync(login.Trim(), ct);
        if (usuario is null || !usuario.Activo || !_hasher.Verificar(contrasena, usuario.HashContrasena))
        {
            await RegistrarAsync(null, login.Trim(), "Intento de inicio de sesión fallido", null, ct);
            return Result.Falla<SesionUsuarioDto>(generico);
        }

        await RegistrarAsync(usuario.Id, usuario.Nombre, "Inicio de sesión", null, ct);

        var permisos = usuario.Rol?.Permisos.Select(p => p.Clave).ToList() ?? new List<string>();
        return Result.Ok(new SesionUsuarioDto(usuario.Id, usuario.Nombre, usuario.UsuarioLogin, permisos));
    }

    /// <summary>
    /// Autorización de supervisor: valida las credenciales de otro usuario y que tenga el
    /// permiso requerido, para autorizar una acción del cajero actual. Devuelve el nombre del
    /// supervisor. No inicia sesión ni cambia el usuario actual.
    /// </summary>
    public async Task<Result<string>> AutorizarAsync(
        string login, string contrasena, string permisoRequerido, string accion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(contrasena))
            return Result.Falla<string>("Escribe usuario y contraseña del supervisor.");

        var usuario = await _usuarios.ObtenerPorLoginAsync(login.Trim(), ct);
        if (usuario is null || !usuario.Activo || !_hasher.Verificar(contrasena, usuario.HashContrasena))
            return Result.Falla<string>("Usuario o contraseña de supervisor incorrectos.");

        var permisos = usuario.Rol?.Permisos.Select(p => p.Clave) ?? Enumerable.Empty<string>();
        if (!permisos.Contains(permisoRequerido))
            return Result.Falla<string>($"«{usuario.Nombre}» no tiene permiso para autorizar esta acción.");

        await RegistrarAsync(usuario.Id, usuario.Nombre, "Autorización de supervisor", accion, ct);
        return Result.Ok(usuario.Nombre);
    }

    // La bitácora se escribe con el usuario en cuestión: durante la autenticación (o la
    // autorización de supervisor) el usuario actual (ICurrentUser) no es quien se valida.
    private async Task RegistrarAsync(Guid? usuarioId, string nombre, string accion, string? detalle, CancellationToken ct)
    {
        try
        {
            await _auditoria.AgregarAsync(new RegistroAuditoria
            {
                Fecha = _reloj.UtcAhora,
                UsuarioId = usuarioId,
                UsuarioNombre = nombre,
                Accion = accion,
                Detalle = detalle
            }, ct);
            await _uow.GuardarCambiosAsync(ct);
        }
        catch
        {
            // Best-effort: no impedir el acceso si falla la bitácora.
        }
    }
}
