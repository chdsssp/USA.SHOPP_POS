using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>
/// Construye el visual de una hoja de etiquetas al tamaño elegido, repartiendo las copias
/// (columnas × filas) y usando la plantilla vertical o la compacta según el cuadrante.
/// El mismo visual sirve para la vista previa y para imprimir.
/// </summary>
public static class EtiquetaVisualFactory
{
    public static FrameworkElement Construir(EtiquetaDatos datos, TamanoEtiqueta tam)
    {
        var grid = new UniformGrid
        {
            Rows = tam.Filas,
            Columns = tam.Columnas,
            Width = tam.AnchoPx,
            Height = tam.AltoPx,
            Background = Brushes.White
        };

        for (var i = 0; i < tam.Copias; i++)
        {
            FrameworkElement etiqueta = tam.CeldaApaisada
                ? new EtiquetaCompactaControl { DataContext = datos }
                : new EtiquetaControl { DataContext = datos };

            grid.Children.Add(new Viewbox
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(3),
                Child = etiqueta
            });
        }

        return grid;
    }
}
