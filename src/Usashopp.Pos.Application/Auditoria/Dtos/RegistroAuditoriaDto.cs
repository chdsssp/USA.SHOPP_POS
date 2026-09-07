namespace Usashopp.Pos.Application.Auditoria.Dtos;

/// <summary>Renglón de la bitácora de auditoría para mostrar.</summary>
public record RegistroAuditoriaDto(
    DateTime Fecha,
    string Usuario,
    string Accion,
    string? Detalle);

/// <summary>Filtros del visor de bitácora.</summary>
public record FiltroAuditoriaDto(
    DateTime? Desde = null,
    DateTime? Hasta = null,
    string? Texto = null);
