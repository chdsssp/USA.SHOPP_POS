namespace Usashopp.Pos.Application.Usuarios.Dtos;

public record SesionUsuarioDto(Guid Id, string Nombre, string Login, IReadOnlyList<string> Permisos);

public record UsuarioDto(Guid Id, string Nombre, string Login, string Rol, Guid RolId, bool Activo);

public record RolDto(Guid Id, string Nombre);

/// <summary>Alta/edición de usuario. Contrasena vacía en edición = no cambiarla.</summary>
public record GuardarUsuarioDto(Guid Id, string Nombre, string Login, Guid RolId, string? Contrasena);
