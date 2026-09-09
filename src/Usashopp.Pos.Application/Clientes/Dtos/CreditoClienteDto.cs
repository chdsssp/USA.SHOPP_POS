namespace Usashopp.Pos.Application.Clientes.Dtos;

/// <summary>Un abono del cliente a su cuenta de crédito.</summary>
public record AbonoClienteDto(DateTime Fecha, decimal Monto, string Metodo, string? Nota);

/// <summary>Estado de crédito (CxC) de un cliente.</summary>
public record EstadoCreditoClienteDto(
    Guid ClienteId,
    string Nombre,
    decimal LimiteCredito,
    decimal Cargos,
    decimal Abonos,
    decimal Saldo,
    decimal Disponible,
    decimal SaldoNotasCredito,
    IReadOnlyList<AbonoClienteDto> AbonosRecientes);

/// <summary>Datos para registrar un abono a la cuenta de crédito del cliente.</summary>
public record RegistrarAbonoClienteDto(Guid ClienteId, decimal Monto, string? Nota = null);
