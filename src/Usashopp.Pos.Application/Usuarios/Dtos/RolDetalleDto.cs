namespace Usashopp.Pos.Application.Usuarios.Dtos;

/// <summary>Renglón del listado de roles.</summary>
public record RolDetalleDto(
    Guid Id,
    string Nombre,
    int Usuarios,
    int Permisos,
    bool EsSistema);

/// <summary>Un permiso del catálogo con su etiqueta y si está asignado a un rol.</summary>
public record PermisoOpcionDto(string Clave, string Etiqueta, bool Asignado);

/// <summary>Alta/edición de un rol con sus permisos (claves).</summary>
public record GuardarRolDto(Guid Id, string Nombre, IReadOnlyList<string> Permisos);
