namespace Usashopp.Pos.Application.Caja.Dtos;

/// <summary>Renglón del historial de cortes de caja (una sesión ya cerrada).</summary>
public record CorteHistorialDto(
    DateTime FechaApertura,
    DateTime? FechaCierre,
    decimal Fondo,
    int NumVentas,
    decimal TotalVentas,
    decimal TotalEfectivo,
    decimal EfectivoEsperado,
    decimal MontoContado,
    decimal Diferencia);
