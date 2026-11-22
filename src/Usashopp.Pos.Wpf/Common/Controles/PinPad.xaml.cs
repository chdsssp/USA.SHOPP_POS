using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Common.Controles;

/// <summary>
/// Teclado de PIN táctil reutilizable (autorización de supervisor, cambio rápido de cajero).
/// Acepta también el teclado físico (dígitos, Retroceso, Enter).
/// </summary>
public partial class PinPad : UserControl
{
    public const int Maximo = 6;

    public static readonly DependencyProperty PinProperty = DependencyProperty.Register(
        nameof(Pin), typeof(string), typeof(PinPad),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((PinPad)d).Actualizar()));

    public string Pin
    {
        get => (string)GetValue(PinProperty);
        set => SetValue(PinProperty, value);
    }

    /// <summary>Se tocó ✓ o se presionó Enter.</summary>
    public event EventHandler<string>? Completado;

    public PinPad()
    {
        InitializeComponent();
        Focusable = true;
        PreviewTextInput += (_, e) =>
        {
            foreach (var c in e.Text.Where(char.IsDigit)) Agregar(c);
            e.Handled = true;
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Back) { Borrar(); e.Handled = true; }
            else if (e.Key is Key.Enter or Key.Return) { Aceptar(); e.Handled = true; }
        };
        Loaded += (_, _) => { Actualizar(); Focus(); };
    }

    public void Limpiar() => Pin = string.Empty;

    private void OnDigito(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string s } && s.Length == 1) Agregar(s[0]);
        Focus();
    }

    private void OnBorrar(object sender, RoutedEventArgs e) { Borrar(); Focus(); }
    private void OnAceptar(object sender, RoutedEventArgs e) => Aceptar();

    private void Agregar(char c)
    {
        if ((Pin ?? "").Length < Maximo) Pin = (Pin ?? "") + c;
    }

    private void Borrar()
    {
        if (!string.IsNullOrEmpty(Pin)) Pin = Pin[..^1];
    }

    private void Aceptar() => Completado?.Invoke(this, Pin ?? string.Empty);

    private void Actualizar() =>
        Puntos.Text = string.IsNullOrEmpty(Pin) ? " " : new string('●', Pin.Length).Replace("●", "● ").TrimEnd();
}
