using System.Globalization;
using System.Text;

namespace Usashopp.Pos.Application.Common;

/// <summary>
/// Búsqueda de texto tolerante: insensible a acentos y mayúsculas, y por varias palabras
/// (cada palabra debe aparecer en alguno de los campos). Se usa para que las barras de búsqueda
/// encuentren por cualquier propiedad del elemento.
/// </summary>
public static class BusquedaTexto
{
    /// <summary>Pasa a minúsculas y quita acentos para comparar de forma tolerante.</summary>
    public static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString();
    }

    /// <summary>Divide el texto de búsqueda en palabras normalizadas.</summary>
    public static string[] Tokens(string? texto) =>
        Normalizar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>True si cada palabra de <paramref name="tokens"/> aparece en la unión de los campos.</summary>
    public static bool Coincide(string[] tokens, params string?[] campos)
    {
        if (tokens.Length == 0) return true;
        var texto = Normalizar(string.Join(" ", campos));
        return tokens.All(texto.Contains);
    }
}
