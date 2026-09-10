using System.Windows;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Comportamientos adjuntos para ventanas. <see cref="RedimensionableProperty"/> permite que una
/// ventana con <c>SizeToContent</c> abra ajustada a su contenido y, tras cargarse, quede
/// libremente redimensionable (fija un tamaño mínimo con el tamaño inicial).
/// </summary>
public static class VentanaHelpers
{
    public static readonly DependencyProperty RedimensionableProperty =
        DependencyProperty.RegisterAttached(
            "Redimensionable", typeof(bool), typeof(VentanaHelpers),
            new PropertyMetadata(false, OnRedimensionableChanged));

    public static bool GetRedimensionable(DependencyObject obj) => (bool)obj.GetValue(RedimensionableProperty);
    public static void SetRedimensionable(DependencyObject obj, bool value) => obj.SetValue(RedimensionableProperty, value);

    private static void OnRedimensionableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window ventana || !(bool)e.NewValue) return;

        ventana.Loaded += (_, _) =>
        {
            // El tamaño auto-calculado por el contenido pasa a ser el mínimo; luego se libera.
            ventana.MinHeight = ventana.ActualHeight;
            if (double.IsNaN(ventana.Width) || ventana.Width <= 0) ventana.MinWidth = ventana.ActualWidth;
            ventana.SizeToContent = SizeToContent.Manual;
        };
    }
}
