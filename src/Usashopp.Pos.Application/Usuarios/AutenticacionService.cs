using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Usuarios.Dtos;

namespace Usashopp.Pos.Application.Usuarios;

/// <summary>Valida credenciales de acceso.</summary>
public class AutenticacionService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;

    public AutenticacionService(IUsuarioRepository usuarios, IPasswordHasher hasher)
    {
        _usuarios = usuarios;
        _hasher = hasher;
    }

    public async Task<Result<SesionUsuarioDto>> ValidarAsync(string login, string contrasena, CancellationToken ct = default)
    {
        const string generico = "Usuario o contraseña incorrectos.";

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(contrasena))
            return Result.Falla<SesionUsuarioDto>(generico);

        var usuario = await _usuarios.ObtenerPorLoginAsync(login.Trim(), ct);
        if (usuario is null || !usuario.Activo)
            return Result.Falla<SesionUsuarioDto>(generico);

        if (!_hasher.Verificar(contrasena, usuario.HashContrasena))
            return Result.Falla<SesionUsuarioDto>(generico);

        var permisos = usuario.Rol?.Permisos.Select(p => p.Clave).ToList() ?? new List<string>();
        return Result.Ok(new SesionUsuarioDto(usuario.Id, usuario.Nombre, usuario.UsuarioLogin, permisos));
    }
}
