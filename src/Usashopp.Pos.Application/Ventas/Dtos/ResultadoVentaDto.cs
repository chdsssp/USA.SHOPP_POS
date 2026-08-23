namespace Usashopp.Pos.Application.Ventas.Dtos;

public record ResultadoVentaDto(
    Guid VentaId,
    string Folio,
    decimal Total,
    decimal Cambio);
