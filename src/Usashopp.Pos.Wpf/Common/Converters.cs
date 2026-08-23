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
