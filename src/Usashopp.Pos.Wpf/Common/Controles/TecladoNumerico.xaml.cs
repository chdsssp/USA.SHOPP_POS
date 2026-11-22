using System.Windows;
using System.Windows.Controls;

namespace Usashopp.Pos.Wpf.Common.Controles;

/// <summary>Teclado numérico en pantalla. Emite teclas; <see cref="TecladoEnPantalla"/> las aplica al campo.</summary>
public partial class TecladoNumerico : UserControl, ITecladoEnPantalla
{
    public event EventHandler<string>? Tecla;

    public TecladoNumerico() => InitializeComponent();

    /// <summary>Oculta el punto decimal en campos de cantidades enteras.</summary>
    public bool PermitirDecimal
    {
        get => TeclaPunto.Visibility == Visibility.Visible;
        set => TeclaPunto.Visibility = value ? Visibility.Visible : Visibility.Hidden;
    }

    private void OnTecla(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string s }) Tecla?.Invoke(this, s);
    }

    private void OnBorrar(object sender, RoutedEventArgs e) => Tecla?.Invoke(this, TeclasEspeciales.Borrar);
    private void OnLimpiar(object sender, RoutedEventArgs e) => Tecla?.Invoke(this, TeclasEspeciales.Limpiar);
    private void OnListo(object sender, RoutedEventArgs e) => Tecla?.Invoke(this, TeclasEspeciales.Listo);
}

/// <summary>Teclado en pantalla que emite teclas (texto o <see cref="TeclasEspeciales"/>).</summary>
public interface ITecladoEnPantalla
{
    event EventHandler<string>? Tecla;
}

public static class TeclasEspeciales
{
    public const string Borrar = "\b";
    public const string Limpiar = "\u0018";
    public const string Listo = "\n";
    public const string Cerrar = "\u001b";
}
