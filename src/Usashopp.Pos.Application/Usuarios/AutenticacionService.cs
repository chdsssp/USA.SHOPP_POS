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
    /// <summary>Reglas del PIN: solo dígitos, de 4 a 6.</summary>
    public static bool PinValido(string? pin) =>
        pin is { Length: >= 4 and <= 6 } && pin.All(char.IsDigit);

    /// <summary>
    /// Desbloqueo de la pantalla: vale el PIN (o contraseña) del mismo usuario que la bloqueó, o el de
    /// alguien con permiso de gestionar usuarios (encargado/administrador).
    /// </summary>
    public async Task<Result> ValidarDesbloqueoAsync(Guid usuarioActual, string? pin, string? login, string? contrasena,
        CancellationToken ct = default)
    {
        Usuario? u = null;
        if (!string.IsNullOrWhiteSpace(pin))
            u = await BuscarPorPinAsync(pin.Trim(), ct);
        else if (!string.IsNullOrWhiteSpace(login) && !string.IsNullOrEmpty(contrasena))
        {
            var candidato = await _usuarios.ObtenerPorLoginAsync(login.Trim(), ct);
            if (candidato is { Activo: true } && _hasher.Verificar(contrasena, candidato.HashContrasena)) u = candidato;
        }
        if (u is null) return Result.Falla("PIN o contraseña incorrectos.");

        var esAdmin = u.Rol?.Permisos.Any(p => p.Clave == Common.Permisos.UsuariosGestionar) == true;
        if (u.Id != usuarioActual && !esAdmin)
            return Result.Falla("Solo quien bloqueó la caja o un encargado puede desbloquearla.");

        await RegistrarAsync(u.Id, u.Nombre, "Desbloqueo de pantalla", null, ct);
        return Result.Ok();
    }

    /// <summary>¿Hay al menos un usuario con PIN? (el login abre en modo PIN).</summary>
    public async Task<bool> HayUsuariosConPinAsync(CancellationToken ct = default) =>
        (await _usuarios.ListarConPinAsync(ct)).Count > 0;

    /// <summary>Usuario activo dueño de ese PIN (los PIN son únicos), o null.</summary>
    private async Task<Usuario?> BuscarPorPinAsync(string pin, CancellationToken ct)
    {
        if (!PinValido(pin)) return null;
        foreach (var u in await _usuarios.ListarConPinAsync(ct))
            if (u.PinHash is { } h && _hasher.Verificar(pin, h))
                return u;
        return null;
    }

    /// <summary>Autorización de supervisor con su PIN (más rápida que usuario + contraseña).</summary>
    public async Task<Result<string>> AutorizarConPinAsync(
        string pin, string permisoRequerido, string accion, CancellationToken ct = default)
    {
        var usuario = await BuscarPorPinAsync(pin, ct);
        if (usuario is null)
            return Result.Falla<string>("PIN incorrecto.");

        var permisos = usuario.Rol?.Permisos.Select(p => p.Clave) ?? Enumerable.Empty<string>();
        if (!permisos.Contains(permisoRequerido))
            return Result.Falla<string>($"«{usuario.Nombre}» no tiene permiso para autorizar esta acción.");

        await RegistrarAsync(usuario.Id, usuario.Nombre, "Autorización de supervisor (PIN)", accion, ct);
        return Result.Ok(usuario.Nombre);
    }

    /// <summary>Inicio de sesión rápido con PIN (cambio de cajero sin escribir usuario y contraseña).</summary>
    public async Task<Result<SesionUsuarioDto>> ValidarPinAsync(string pin, CancellationToken ct = default)
    {
        var usuario = await BuscarPorPinAsync(pin, ct);
        if (usuario is null)
        {
            await RegistrarAsync(null, "(PIN)", "Intento de inicio de sesión con PIN fallido", null, ct);
            return Result.Falla<SesionUsuarioDto>("PIN incorrecto.");
        }
        await RegistrarAsync(usuario.Id, usuario.Nombre, "Inicio de sesión (PIN)", null, ct);
        var permisos = usuario.Rol?.Permisos.Select(p => p.Clave).ToList() ?? new List<string>();
        return Result.Ok(new SesionUsuarioDto(usuario.Id, usuario.Nombre, usuario.UsuarioLogin, permisos));
    }

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
