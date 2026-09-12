using System.Windows;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Comportamientos adjuntos para ventanas. <see cref="RedimensionableProperty"/> hace que una
/// ventana con <c>SizeToContent</c> abra ajustada a su contenido (mostrando todos los elementos)
/// y, una vez renderizada, quede libremente redimensionable, fijando ese tamaño inicial como
/// mínimo para que nunca se encoja por debajo de lo que muestra el contenido completo.
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

        // ContentRendered se dispara tras el primer render completo: el alto ya es el del contenido.
        ventana.ContentRendered += (_, _) =>
        {
            if (ventana.SizeToContent == SizeToContent.Manual) return;

            var alto = ventana.ActualHeight;
            var ancho = ventana.ActualWidth;

            // Libera el auto-tamaño y fija el tamaño renderizado como mínimo (deja crecer/encoger).
            ventana.SizeToContent = SizeToContent.Manual;
            ventana.MinHeight = alto;
            ventana.MinWidth = ancho;
            ventana.Height = alto;
            ventana.Width = ancho;
        };
    }
}
