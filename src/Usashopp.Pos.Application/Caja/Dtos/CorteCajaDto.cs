namespace Usashopp.Pos.Application.Caja.Dtos;

/// <summary>Resumen para el corte de caja de la sesión abierta.</summary>
public record CorteCajaDto(
    decimal Fondo,
    int NumVentas,
    decimal TotalVentas,
    decimal TotalEfectivo,
    decimal EfectivoEsperado);
