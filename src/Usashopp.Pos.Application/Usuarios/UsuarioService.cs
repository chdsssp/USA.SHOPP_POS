using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Usuarios;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IRolRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public UsuarioService(IUsuarioRepository usuarios, IRolRepository roles, IPasswordHasher hasher,
        IUnitOfWork uow, IAuditoria auditoria)
    {
        _usuarios = usuarios;
        _roles = roles;
        _hasher = hasher;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _usuarios.ListarConRolAsync(ct);
        return lista.Select(u => new UsuarioDto(u.Id, u.Nombre, u.UsuarioLogin, u.Rol?.Nombre ?? "—", u.RolId, u.Activo)).ToList();
    }

    public async Task<IReadOnlyList<RolDto>> ListarRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roles.ListarAsync(null, ct);
        return roles.OrderBy(r => r.Nombre).Select(r => new RolDto(r.Id, r.Nombre)).ToList();
    }

    public async Task<Result> CrearAsync(GuardarUsuarioDto dto, CancellationToken ct = default)
    {
        var error = Validar(dto, esNuevo: true);
        if (error is not null) return Result.Falla(error);

        if (await _usuarios.ExisteLoginAsync(dto.Login.Trim(), null, ct))
            return Result.Falla($"El usuario «{dto.Login}» ya existe.");

        await _usuarios.AgregarAsync(new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            UsuarioLogin = dto.Login.Trim(),
            HashContrasena = _hasher.Hash(dto.Contrasena!),
            RolId = dto.RolId
        }, ct);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Alta de usuario", $"Usuario «{dto.Login.Trim()}»", "Usuario");
        return Result.Ok();
    }

    public async Task<Result> ActualizarAsync(GuardarUsuarioDto dto, CancellationToken ct = default)
    {
        var error = Validar(dto, esNuevo: false);
        if (error is not null) return Result.Falla(error);

        var usuario = await _usuarios.ObtenerPorIdAsync(dto.Id, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");

        if (await _usuarios.ExisteLoginAsync(dto.Login.Trim(), dto.Id, ct))
            return Result.Falla($"El usuario «{dto.Login}» ya existe.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.UsuarioLogin = dto.Login.Trim();
        usuario.RolId = dto.RolId;
        if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            usuario.HashContrasena = _hasher.Hash(dto.Contrasena);
        _usuarios.Actualizar(usuario);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Edición de usuario", $"Usuario «{dto.Login.Trim()}»", "Usuario", usuario.Id);
        return Result.Ok();
    }

    /// <summary>Permite al usuario autenticado cambiar su propia contraseña (verifica la actual).</summary>
    public async Task<Result> CambiarMiContrasenaAsync(Guid usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contrasenaActual))
            return Result.Falla("Escribe tu contraseña actual.");
        if (string.IsNullOrWhiteSpace(contrasenaNueva) || contrasenaNueva.Length < 4)
            return Result.Falla("La nueva contraseña debe tener al menos 4 caracteres.");

        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");

        if (!_hasher.Verificar(contrasenaActual, usuario.HashContrasena))
            return Result.Falla("La contraseña actual no es correcta.");

        usuario.HashContrasena = _hasher.Hash(contrasenaNueva);
        _usuarios.Actualizar(usuario);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Cambio de contraseña propia", null, "Usuario", usuario.Id);
        return Result.Ok();
    }

    /// <summary>
    /// Fija o quita (pin vacío) el PIN de un usuario. 4–6 dígitos y único: dos usuarios no pueden
    /// compartir PIN, porque el PIN identifica a la persona al autorizar o cambiar de cajero.
    /// </summary>
    public async Task<Result> EstablecerPinAsync(Guid usuarioId, string? pin, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");

        if (string.IsNullOrWhiteSpace(pin))
        {
            usuario.PinHash = null;
        }
        else
        {
            pin = pin.Trim();
            if (!AutenticacionService.PinValido(pin))
                return Result.Falla("El PIN debe tener de 4 a 6 dígitos.");
            foreach (var otro in await _usuarios.ListarConPinAsync(ct))
                if (otro.Id != usuarioId && otro.PinHash is { } h && _hasher.Verificar(pin, h))
                    return Result.Falla("Ese PIN ya lo usa otra persona. Elige otro.");
            usuario.PinHash = _hasher.Hash(pin);
        }

        _usuarios.Actualizar(usuario);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync(pin is null ? "PIN eliminado" : "PIN establecido", $"Usuario «{usuario.UsuarioLogin}»", "Usuario", usuario.Id);
        return Result.Ok();
    }

    /// <summary>El propio usuario fija su PIN; confirma su identidad con la contraseña actual.</summary>
    public async Task<Result> EstablecerMiPinAsync(Guid usuarioId, string contrasenaActual, string? pin, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");
        if (string.IsNullOrWhiteSpace(contrasenaActual) || !_hasher.Verificar(contrasenaActual, usuario.HashContrasena))
            return Result.Falla("Escribe tu contraseña actual correcta para cambiar el PIN.");
        return await EstablecerPinAsync(usuarioId, pin, ct);
    }

    /// <summary>Id de un usuario por su login (para fijar el PIN justo después de crearlo).</summary>
    public async Task<Guid?> ObtenerIdPorLoginAsync(string login, CancellationToken ct = default) =>
        (await _usuarios.ObtenerPorLoginAsync(login.Trim(), ct))?.Id;

    /// <summary>Si el usuario tiene PIN configurado.</summary>
    public async Task<bool> TienePinAsync(Guid usuarioId, CancellationToken ct = default) =>
        (await _usuarios.ObtenerPorIdAsync(usuarioId, ct))?.PinHash is not null;

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");
        usuario.Activo = false;
        _usuarios.Actualizar(usuario);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Baja de usuario", $"Usuario «{usuario.UsuarioLogin}»", "Usuario", usuario.Id);
        return Result.Ok();
    }

    private static string? Validar(GuardarUsuarioDto dto, bool esNuevo)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return "El nombre es obligatorio.";
        if (string.IsNullOrWhiteSpace(dto.Login)) return "El usuario (login) es obligatorio.";
        if (dto.RolId == Guid.Empty) return "Selecciona un rol.";
        if (esNuevo && string.IsNullOrWhiteSpace(dto.Contrasena)) return "La contraseña es obligatoria.";
        return null;
    }
}
