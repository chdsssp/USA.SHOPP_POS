using System.Collections.Generic;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>
/// Un tamaño de papel/etiqueta y cómo se reparten las copias en la hoja (columnas × filas).
/// Las medidas están en px a 96 DPI (1" = 96 px, 1 mm = 96/25.4 px).
/// </summary>
public record TamanoEtiqueta(string Nombre, double AnchoPx, double AltoPx, int Columnas, int Filas)
{
    public int Copias => Columnas * Filas;

    /// <summary>Un cuadrante es apaisado si es más ancho que alto (usa la plantilla compacta).</summary>
    public bool CeldaApaisada => (AnchoPx / Columnas) > (AltoPx / Filas);

    public override string ToString() => Nombre;

    private const double Pulgada = 96.0;
    private const double Mm = 96.0 / 25.4;

    public static IReadOnlyList<TamanoEtiqueta> Todos { get; } = new[]
    {
        new TamanoEtiqueta("4×6\" — 4 etiquetas (2×2)", 4 * Pulgada, 6 * Pulgada, 2, 2),
        new TamanoEtiqueta("4×6\" — 2 etiquetas (1×2)", 4 * Pulgada, 6 * Pulgada, 1, 2),
        new TamanoEtiqueta("4×6\" — 1 etiqueta",        4 * Pulgada, 6 * Pulgada, 1, 1),
        new TamanoEtiqueta("4×2\" — 1 etiqueta",        4 * Pulgada, 2 * Pulgada, 1, 1),
        new TamanoEtiqueta("3×2\" — 1 etiqueta",        3 * Pulgada, 2 * Pulgada, 1, 1),
        new TamanoEtiqueta("2×1\" — 1 etiqueta",        2 * Pulgada, 1 * Pulgada, 1, 1),
        new TamanoEtiqueta("80 mm × 50 mm",             80 * Mm,     50 * Mm,     1, 1),
        new TamanoEtiqueta("80 mm × 100 mm",            80 * Mm,     100 * Mm,    1, 1),
    };
}
