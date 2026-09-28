using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>Dibuja un código de barras Code 128 a partir de la propiedad <see cref="Codigo"/>.</summary>
public class BarcodeControl : FrameworkElement
{
    public static readonly DependencyProperty CodigoProperty =
        DependencyProperty.Register(
            nameof(Codigo), typeof(string), typeof(BarcodeControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Codigo
    {
        get => (string)GetValue(CodigoProperty);
        set => SetValue(CodigoProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var ancho = ActualWidth;
        var alto = ActualHeight;
        if (ancho <= 0 || alto <= 0) return;

        // Fondo blanco para que imprima limpio y sea escaneable.
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ancho, alto));

        var codigo = Codigo;
        if (string.IsNullOrWhiteSpace(codigo)) return;

        var modulos = Code128.Modulos(codigo);
        var totalModulos = modulos.Sum();
        if (totalModulos <= 0) return;

        var anchoModulo = ancho / totalModulos;
        double x = 0;
        for (var i = 0; i < modulos.Count; i++)
        {
            var w = modulos[i] * anchoModulo;
            if (i % 2 == 0) // índice par = barra
                dc.DrawRectangle(Brushes.Black, null, new Rect(x, 0, w, alto));
            x += w;
        }
    }
}
