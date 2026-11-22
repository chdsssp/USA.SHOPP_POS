using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Presenta cualquier ventana de diálogo como una "hoja" táctil a pantalla completa: fondo oscurecido,
/// tarjeta centrada con el contenido original y una barra superior con un botón grande "← Volver".
/// Así las 30+ ventanas de gestión (producto, cliente, compra, corte…) se usan igual con el dedo sin
/// reescribir cada una, y nunca quedan detrás de la ventana principal ni fuera de la pantalla.
/// Se desactiva por equipo (<see cref="Activo"/>) y no toca ventanas ya maximizadas/sin borde.
/// </summary>
public static class HojaTactil
{
    /// <summary>Preferencia del equipo (Configuración → Este equipo).</summary>
    public static bool Activo { get; set; } = true;

    /// <summary>Muestra la ventana como diálogo (hoja táctil si está activo). Igual que ShowDialog.</summary>
    public static bool? Mostrar(Window ventana)
    {
        if (Activo) Preparar(ventana);
        return ventana.ShowDialog();
    }

    /// <summary>Envuelve el contenido de la ventana en la hoja. Debe llamarse antes de mostrarla.</summary>
    public static void Preparar(Window w)
    {
        if (w.WindowState == WindowState.Maximized || w.WindowStyle == WindowStyle.None) return;
        if (w.Content is not UIElement contenido) return;

        var app = System.Windows.Application.Current;
        T R<T>(string clave) => (T)app.FindResource(clave);

        var area = SystemParameters.WorkArea;
        var ajustaAlto = w.SizeToContent is SizeToContent.Height or SizeToContent.WidthAndHeight || double.IsNaN(w.Height);
        var ajustaAncho = w.SizeToContent is SizeToContent.Width or SizeToContent.WidthAndHeight || double.IsNaN(w.Width);
        var fondo = w.Background;

        w.Content = null;

        var volver = new Button
        {
            Content = "←  Volver",
            Style = R<Style>("Button.Secondary"),
            MinWidth = 140,
            MinHeight = R<double>("TouchTarget.Comodo"),
            IsCancel = true
        };
        volver.Click += (_, _) => w.Close();

        var titulo = new TextBlock
        {
            Text = w.Title,
            FontSize = R<double>("Font.Subtitle"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var cabecera = new DockPanel { Margin = new Thickness(12, 10, 12, 0) };
        DockPanel.SetDock(volver, Dock.Left);
        cabecera.Children.Add(volver);
        cabecera.Children.Add(titulo);
        DockPanel.SetDock(cabecera, Dock.Top);

        UIElement cuerpo = ajustaAlto
            ? new ScrollViewer { Content = contenido, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }
            : contenido;

        var panel = new DockPanel();
        panel.Children.Add(cabecera);
        panel.Children.Add(cuerpo);

        var tarjeta = new Border
        {
            Background = fondo ?? R<Brush>("Brush.Bg.App"),
            BorderBrush = R<Brush>("Brush.Border.Strong"),
            BorderThickness = new Thickness(1),
            CornerRadius = R<CornerRadius>("Radius.Lg"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipToBounds = true,
            MaxWidth = Math.Max(320, area.Width - 32),
            MaxHeight = Math.Max(320, area.Height - 32),
            MinWidth = Math.Min(w.MinWidth, area.Width - 32),
            Child = panel
        };
        if (!ajustaAncho) tarjeta.Width = Math.Min(w.Width, area.Width - 32);
        if (!ajustaAlto) tarjeta.Height = Math.Min(w.Height, area.Height - 32);

        var raiz = new Grid { Background = R<Brush>("Brush.Overlay") };
        raiz.Children.Add(tarjeta);

        w.WindowStyle = WindowStyle.None;
        w.AllowsTransparency = true;
        w.Background = Brushes.Transparent;
        w.ResizeMode = ResizeMode.NoResize;
        w.SizeToContent = SizeToContent.Manual;
        w.ShowInTaskbar = false;
        w.MinWidth = 0;
        w.MinHeight = 0;
        w.WindowState = WindowState.Maximized;
        w.Content = raiz;

        // Esc = Volver (si el contenido no lo usó).
        w.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !e.Handled) { e.Handled = true; w.Close(); }
        };
    }
}
