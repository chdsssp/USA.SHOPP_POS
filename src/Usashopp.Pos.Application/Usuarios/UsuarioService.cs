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

    public UsuarioService(IUsuarioRepository usuarios, IRolRepository roles, IPasswordHasher hasher, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _roles = roles;
        _hasher = hasher;
        _uow = uow;
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
        return Result.Ok();
    }

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, ct);
        if (usuario is null) return Result.Falla("El usuario no existe.");
        usuario.Activo = false;
        _usuarios.Actualizar(usuario);
        await _uow.GuardarCambiosAsync(ct);
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
