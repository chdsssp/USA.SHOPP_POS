using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>Obtiene hasta 2 iniciales del nombre del producto (para la miniatura).</summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var texto = (value as string ?? string.Empty).Split('—')[0].Trim();
        var partes = texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var iniciales = string.Empty;
        foreach (var p in partes)
        {
            if (char.IsLetterOrDigit(p[0])) iniciales += char.ToUpper(p[0], culture);
            if (iniciales.Length == 2) break;
        }
        return iniciales.Length == 0 ? "?" : iniciales;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Color estable derivado del texto, tomado de una paleta (miniaturas de color).</summary>
public class ColorFromStringConverter : IValueConverter
{
    private static readonly string[] Paleta =
    {
        "#7C3AED", "#2C6ECB", "#007F5F", "#D72C0D",
        "#B98900", "#0EA5B5", "#C026D3", "#475569"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var texto = value as string ?? string.Empty;
        var hash = 0;
        foreach (var c in texto) hash += c;
        var hex = Paleta[Math.Abs(hash) % Paleta.Length];
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Colapsa el elemento cuando el valor es null o cadena vacía.</summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vacio = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
        // ConverterParameter="inv" invierte: visible cuando ESTÁ vacío.
        if (parameter is string p && p == "inv")
            return vacio ? Visibility.Visible : Visibility.Collapsed;
        return vacio ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Invierte un booleano (útil para IsEnabled durante estados "ocupado").</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}

/// <summary>Muestra el elemento cuando un contador es 0 (útil para estados vacíos).</summary>
public class CountZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Muestra el elemento cuando el booleano es FALSE (colapsa cuando es true).</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// true si el valor es igual al parámetro (comparación invariante). De regreso, al marcarse
/// devuelve el parámetro convertido al tipo destino: sirve para grupos de RadioButton/segmentos.
/// </summary>
public class IgualAConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture),
                      System.Convert.ToString(parameter, CultureInfo.InvariantCulture),
                      StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true || parameter is null) return Binding.DoNothing;
        var tipo = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (tipo.IsEnum) return Enum.Parse(tipo, parameter.ToString()!, ignoreCase: true);
        return System.Convert.ChangeType(parameter, tipo, CultureInfo.InvariantCulture);
    }
}

/// <summary>Ruta de imagen (absoluta) → BitmapImage reducida y en caché; null si no hay o falla.</summary>
public class RutaAImagenConverter : IValueConverter
{
    /// <summary>Ancho de decodificación: evita cargar fotos de 12 MP completas en memoria.</summary>
    public int AnchoDecodificado { get; set; } = 240;

    private static readonly Dictionary<string, System.Windows.Media.Imaging.BitmapImage> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string ruta || string.IsNullOrWhiteSpace(ruta) || !System.IO.File.Exists(ruta))
            return null;
        var clave = ruta + "|" + AnchoDecodificado;
        lock (Cache)
        {
            if (Cache.TryGetValue(clave, out var enCache)) return enCache;
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // no bloquea el archivo
                bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile;
                bmp.DecodePixelWidth = AnchoDecodificado;
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                if (Cache.Count > 500) Cache.Clear();
                Cache[clave] = bmp;
                return bmp;
            }
            catch { return null; }
        }
    }

    /// <summary>Olvida una imagen (cuando se reemplaza la foto del producto).</summary>
    public static void Invalidar(string? ruta)
    {
        if (ruta is null) return;
        lock (Cache)
            foreach (var k in Cache.Keys.Where(k => k.StartsWith(ruta + "|")).ToList()) Cache.Remove(k);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
