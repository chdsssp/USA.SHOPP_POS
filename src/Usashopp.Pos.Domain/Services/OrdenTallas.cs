using System.Globalization;

namespace Usashopp.Pos.Domain.Services;

/// <summary>
/// Orden lógico de tallas para mostrarlas de menor a mayor (no alfabético): XS, S/CH, M, L/G,
/// XL/EG… y las numéricas de menor a mayor (pantalón 28, 30, 32; calzado 22.5, 23…).
/// </summary>
public static class OrdenTallas
{
    private static readonly string[] Letras =
    {
        "RN", "XXXS", "3XS", "XXS", "2XS", "XS", "XCH", "S", "CH", "M", "MED", "L", "G", "XL", "EG", "XG",
        "XXL", "2XL", "EEG", "XXG", "XXXL", "3XL", "XXXXL", "4XL", "5XL", "UNI", "UNITALLA", "U"
    };

    /// <summary>Clave de orden: primero tallas de letra conocidas, luego numéricas, luego el resto alfabético.</summary>
    public static (int Grupo, decimal Valor, string Texto) Clave(string? talla)
    {
        var t = (talla ?? string.Empty).Trim().ToUpperInvariant();
        if (t.Length == 0) return (3, 0, string.Empty);
        var i = Array.IndexOf(Letras, t);
        if (i >= 0) return (0, i, t);
        var numero = new string(t.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
        if (numero.Length > 0 && decimal.TryParse(numero, NumberStyles.Number, CultureInfo.InvariantCulture, out var n))
            return (1, n, t);
        return (2, 0, t);
    }

    public static IEnumerable<string> Ordenar(IEnumerable<string> tallas) =>
        tallas.OrderBy(t => Clave(t).Grupo).ThenBy(t => Clave(t).Valor).ThenBy(t => Clave(t).Texto, StringComparer.Ordinal);
}
