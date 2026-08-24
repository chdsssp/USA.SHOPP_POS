namespace Usashopp.Pos.Application.Inventario.Dtos;

/// <summary>Renglón del kardex: un movimiento de inventario con el saldo resultante.</summary>
public record MovimientoKardexDto(
    DateTime Fecha,
    string Tipo,
    int Cantidad,
    int Saldo,
    string? Motivo);
