using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Ajustes globales para pantalla táctil, aplicados a TODAS las ventanas al cargarse:
/// <list type="bullet">
/// <item>Escala de la interfaz por equipo (<see cref="ConfiguracionTerminal.EscalaUi"/>).</item>
/// <item>Sin "mantener presionado = clic derecho" ni gestos flick de Windows (estorban en el POS).</item>
/// <item>Sin el "rebote" de toda la ventana al llegar al final de una lista con el dedo.</item>
/// </list>
/// </summary>
public static class AjustesTactiles
{
    private static readonly ScaleTransform Escala = new(1, 1);

    public static void Registrar(ConfiguracionTerminal terminal)
    {
        AplicarEscala(terminal.EscalaUi);
        TecladoEnPantalla.Modo = terminal.Teclado;
        terminal.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConfiguracionTerminal.EscalaUi)) AplicarEscala(terminal.EscalaUi);
            if (e.PropertyName == nameof(ConfiguracionTerminal.Teclado)) TecladoEnPantalla.Modo = terminal.Teclado;
        };

        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((s, _) => Preparar((Window)s)));

        EventManager.RegisterClassHandler(typeof(Window), UIElement.ManipulationBoundaryFeedbackEvent,
            new EventHandler<ManipulationBoundaryFeedbackEventArgs>((_, e) => e.Handled = true));
    }

    private static void AplicarEscala(double factor)
    {
        Escala.ScaleX = factor;
        Escala.ScaleY = factor;
    }

    private static void Preparar(Window w)
    {
        Stylus.SetIsPressAndHoldEnabled(w, false);
        Stylus.SetIsFlicksEnabled(w, false);
        Stylus.SetIsTapFeedbackEnabled(w, true);
        if (w.Content is FrameworkElement raiz && raiz.LayoutTransform != Escala)
            raiz.LayoutTransform = Escala;
    }
}
