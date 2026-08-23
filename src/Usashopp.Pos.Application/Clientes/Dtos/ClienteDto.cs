namespace Usashopp.Pos.Application.Clientes.Dtos;

public record ClienteDto(
    Guid Id,
    string Nombre,
    string? Telefono,
    string? Email,
    string? Notas,
    bool Activo);
