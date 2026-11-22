using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Usashopp.Pos.Wpf.Common.Controles;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Teclados en pantalla propios de la app (el teclado táctil de Windows aparece de forma poco
/// confiable en WPF). Se activan con <c>TecladoEnPantalla.Auto="True"</c> (ya puesto en los estilos
/// de TextBox/PasswordBox): los campos con <c>InputHelpers.SoloNumeros</c> abren el numérico y los
/// demás el alfabético. <c>Numerico</c>/<c>Alfabetico</c> fuerzan uno.
/// Cuándo aparecen lo decide <see cref="Modo"/> (preferencia de "Este equipo").
/// </summary>
public static class TecladoEnPantalla
{
    /// <summary>Modo global (lo fija la app desde <see cref="ConfiguracionTerminal"/>).</summary>
    public static ModoTeclado Modo { get; set; } = ModoTeclado.SoloTactil;

    private enum Tipo { Ninguno, Numerico, Alfabetico }

    // ---------- Propiedades adjuntas ----------

    public static readonly DependencyProperty AutoProperty = DependencyProperty.RegisterAttached(
        "Auto", typeof(bool), typeof(TecladoEnPantalla), new PropertyMetadata(false, OnCambio));
    public static bool GetAuto(DependencyObject d) => (bool)d.GetValue(AutoProperty);
    public static void SetAuto(DependencyObject d, bool v) => d.SetValue(AutoProperty, v);

    public static readonly DependencyProperty NumericoProperty = DependencyProperty.RegisterAttached(
        "Numerico", typeof(bool), typeof(TecladoEnPantalla), new PropertyMetadata(false, OnCambio));
    public static bool GetNumerico(DependencyObject d) => (bool)d.GetValue(NumericoProperty);
    public static void SetNumerico(DependencyObject d, bool v) => d.SetValue(NumericoProperty, v);

    public static readonly DependencyProperty AlfabeticoProperty = DependencyProperty.RegisterAttached(
        "Alfabetico", typeof(bool), typeof(TecladoEnPantalla), new PropertyMetadata(false, OnCambio));
    public static bool GetAlfabetico(DependencyObject d) => (bool)d.GetValue(AlfabeticoProperty);
    public static void SetAlfabetico(DependencyObject d, bool v) => d.SetValue(AlfabeticoProperty, v);

    /// <summary>Desactiva el teclado en un campo concreto aunque el estilo lo active.</summary>
    public static readonly DependencyProperty DesactivadoProperty = DependencyProperty.RegisterAttached(
        "Desactivado", typeof(bool), typeof(TecladoEnPantalla), new PropertyMetadata(false));
    public static bool GetDesactivado(DependencyObject d) => (bool)d.GetValue(DesactivadoProperty);
    public static void SetDesactivado(DependencyObject d, bool v) => d.SetValue(DesactivadoProperty, v);

    private static void OnCambio(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el) return;
        el.PreviewTouchUp -= OnToque;
        el.PreviewMouseLeftButtonUp -= OnClic;
        if (GetAuto(d) || GetNumerico(d) || GetAlfabetico(d))
        {
            el.PreviewTouchUp += OnToque;
            el.PreviewMouseLeftButtonUp += OnClic;
        }
    }

    // ---------- Apertura ----------

    private static void OnToque(object sender, TouchEventArgs e)
    {
        if (Modo == ModoTeclado.Nunca) return;
        Abrir((Control)sender);
    }

    private static void OnClic(object sender, MouseButtonEventArgs e)
    {
        // El toque también genera un clic "promovido": solo cuenta como mouse real si no viene de stylus.
        if (e.StylusDevice is not null) return;
        if (Modo == ModoTeclado.Siempre) Abrir((Control)sender);
    }

    private static Popup? _popup;
    private static Control? _destino;

    private static Tipo TipoPara(Control c)
    {
        if (GetDesactivado(c)) return Tipo.Ninguno;
        if (GetNumerico(c)) return Tipo.Numerico;
        if (GetAlfabetico(c)) return Tipo.Alfabetico;
        if (!GetAuto(c)) return Tipo.Ninguno;
        if (c is TextBox { IsReadOnly: true }) return Tipo.Ninguno;
        return InputHelpers.GetSoloNumeros(c) ? Tipo.Numerico : Tipo.Alfabetico;
    }

    /// <summary>Abre el teclado adecuado debajo (o encima) del campo.</summary>
    public static void Abrir(Control campo)
    {
        var tipo = TipoPara(campo);
        if (tipo == Tipo.Ninguno || !campo.IsEnabled) return;

        campo.Focus();
        if (ReferenceEquals(_destino, campo) && _popup?.IsOpen == true) return;
        Cerrar();

        ITecladoEnPantalla teclado = tipo == Tipo.Numerico
            ? new TecladoNumerico { PermitirDecimal = PermiteDecimal(campo) }
            : new TecladoAlfabetico();
        teclado.Tecla += (_, t) => Aplicar(t);

        _destino = campo;
        _popup = new Popup
        {
            Child = (UIElement)teclado,
            PlacementTarget = campo,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 6,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None,
            IsOpen = true
        };
        _popup.Closed += (_, _) => { if (ReferenceEquals(_popup?.Child, teclado)) { _popup = null; _destino = null; } };

        // En campos numéricos la primera tecla reemplaza el valor (como en una calculadora).
        if (tipo == Tipo.Numerico && campo is TextBox tb) tb.SelectAll();
    }

    public static void Cerrar()
    {
        if (_popup is { } p) p.IsOpen = false;
        _popup = null;
        _destino = null;
    }

    private static bool PermiteDecimal(Control c)
    {
        // Cantidades enteras (binding a int) no necesitan punto decimal.
        var expr = (c as TextBox)?.GetBindingExpression(TextBox.TextProperty);
        var ruta = expr?.ParentBinding?.Path?.Path ?? string.Empty;
        return !ruta.Contains("Cantidad", StringComparison.OrdinalIgnoreCase) &&
               !ruta.Contains("Stock", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Escritura en el campo ----------

    private static void Aplicar(string tecla)
    {
        switch (_destino)
        {
            case TextBox tb:
                AplicarTextBox(tb, tecla);
                break;
            case PasswordBox pb:
                if (tecla == TeclasEspeciales.Borrar) { if (pb.Password.Length > 0) pb.Password = pb.Password[..^1]; }
                else if (tecla == TeclasEspeciales.Limpiar) pb.Password = string.Empty;
                else if (tecla == TeclasEspeciales.Listo) Terminar(pb);
                else pb.Password += tecla;
                break;
        }
    }

    private static void AplicarTextBox(TextBox tb, string tecla)
    {
        if (tecla == TeclasEspeciales.Listo) { Terminar(tb); return; }
        if (tecla == TeclasEspeciales.Limpiar) { tb.Clear(); return; }

        var inicio = tb.SelectionStart;
        var largo = tb.SelectionLength;
        var texto = tb.Text ?? string.Empty;

        if (tecla == TeclasEspeciales.Borrar)
        {
            if (largo > 0) { tb.Text = texto.Remove(inicio, largo); tb.CaretIndex = inicio; }
            else if (inicio > 0) { tb.Text = texto.Remove(inicio - 1, 1); tb.CaretIndex = inicio - 1; }
            return;
        }

        if (tb.MaxLength > 0 && texto.Length - largo + tecla.Length > tb.MaxLength) return;
        tb.Text = texto.Remove(inicio, largo).Insert(inicio, tecla);
        tb.CaretIndex = inicio + tecla.Length;
    }

    /// <summary>"Listo": cierra y envía Enter al campo (dispara búsquedas o confirma, como el teclado físico).</summary>
    private static void Terminar(Control campo)
    {
        Cerrar();
        var fuente = PresentationSource.FromVisual(campo);
        if (fuente is null) return;
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, fuente, 0, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        campo.RaiseEvent(args);
        if (args.Handled) return;
        args = new KeyEventArgs(Keyboard.PrimaryDevice, fuente, 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent };
        campo.RaiseEvent(args);
    }
}
