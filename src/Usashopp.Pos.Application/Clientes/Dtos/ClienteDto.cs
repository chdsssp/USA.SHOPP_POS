namespace Usashopp.Pos.Application.Clientes.Dtos;

public record ClienteDto(
    Guid Id,
    string Nombre,
    string? Telefono,
    string? Email,
    string? Notas,
    bool Activo,
    string? Rfc = null,
    string? RazonSocial = null,
    string? RegimenFiscal = null,
    string? UsoCfdi = null,
    string? DireccionFiscal = null,
    decimal LimiteCredito = 0,
    int Puntos = 0);
