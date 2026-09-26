namespace Usashopp.Pos.Application.Common;

/// <summary>
/// Conversión entre la hora local (la que ve y captura el usuario) y UTC (como se guardan las
/// fechas de las ventas). Evita que los filtros por fecha o la agrupación por hora se corran por
/// la diferencia horaria.
/// </summary>
public static class Fechas
{
    /// <summary>Interpreta una fecha local (p. ej. de un selector) como su instante en UTC.</summary>
    public static DateTime? LocalAUtc(DateTime? local) =>
        local is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Local).ToUniversalTime() : null;

    /// <summary>Convierte una marca de tiempo UTC (de la base) a hora local para mostrar o agrupar.</summary>
    public static DateTime UtcALocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
