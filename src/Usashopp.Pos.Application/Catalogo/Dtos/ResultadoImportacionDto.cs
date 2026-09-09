namespace Usashopp.Pos.Application.Catalogo.Dtos;

/// <summary>Resumen de una importación de catálogo desde CSV.</summary>
public record ResultadoImportacionDto(
    int Creados,
    int Actualizados,
    int Omitidos,
    IReadOnlyList<string> Errores);
