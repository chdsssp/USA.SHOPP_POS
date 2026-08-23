namespace Usashopp.Pos.Application.Proveedores.Dtos;

public record ProveedorDto(
    Guid Id,
    string Nombre,
    string? Contacto,
    string? Telefono,
    string? Email,
    bool Activo);
