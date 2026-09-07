namespace Usashopp.Pos.Application.Ventas.Dtos;

/// <summary>Nota de crédito (saldo a favor) de un cliente.</summary>
public record NotaCreditoDto(
    Guid Id,
    string Folio,
    DateTime Fecha,
    decimal Monto,
    decimal Saldo,
    string Estado);
