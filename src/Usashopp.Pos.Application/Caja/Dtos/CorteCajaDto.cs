namespace Usashopp.Pos.Application.Caja.Dtos;

/// <summary>Resumen para el corte de caja de la sesión abierta.</summary>
public record CorteCajaDto(
    decimal Fondo,
    int NumVentas,
    decimal TotalVentas,
    decimal TotalEfectivo,
    decimal Ingresos,
    decimal Salidas,
    decimal EfectivoEsperado);

/// <summary>Un movimiento de efectivo de la caja (ingreso, retiro, gasto, reembolso).</summary>
public record MovimientoCajaDto(DateTime Fecha, string Tipo, decimal Monto, decimal Efecto, string? Concepto);
