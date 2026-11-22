using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Common.Controles;

/// <summary>
/// Teclado alfabético en pantalla (español MX): ñ, mayúsculas, acento (´ + vocal), números y
/// símbolos comunes (@ . - _). No depende del teclado táctil de Windows, que en WPF aparece de
/// forma poco confiable.
/// </summary>
public partial class TecladoAlfabetico : UserControl, ITecladoEnPantalla
{
    public event EventHandler<string>? Tecla;

    private static readonly string[][] Letras =
    {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
        new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "ñ" },
        new[] { "z", "x", "c", "v", "b", "n", "m", ",", ".", "-" },
    };

    private static readonly string[][] Simbolos =
    {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "@", "#", "$", "%", "&", "*", "(", ")", "/", "\"" },
        new[] { "+", "=", "_", ":", ";", "!", "?", "¿", "¡", "'" },
        new[] { "á", "é", "í", "ó", "ú", "ü", "°", ",", ".", "-" },
    };

    private bool _mayusculas;
    private bool _acento;
    private bool _simbolos;

    public TecladoAlfabetico()
    {
        InitializeComponent();
        Construir();
    }

    private void Construir()
    {
        Filas.Children.Clear();
        foreach (var fila in _simbolos ? Simbolos : Letras)
        {
            var g = new UniformGrid { Rows = 1, Columns = fila.Length };
            foreach (var t in fila) g.Children.Add(Boton(_mayusculas ? t.ToUpperInvariant() : t, OnCaracter));
            Filas.Children.Add(g);
        }

        var inferior = new Grid();
        void Col(double estrellas) => inferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(estrellas, GridUnitType.Star) });
        Col(1.4); Col(1); Col(1); Col(4); Col(1.4); Col(1.8);
        Agregar(inferior, 0, Boton(_mayusculas ? "⇧ ABC" : "⇧ abc", (_, _) => { _mayusculas = !_mayusculas; Construir(); }, activo: _mayusculas));
        Agregar(inferior, 1, Boton("´", (_, _) => { _acento = !_acento; Construir(); }, activo: _acento));
        Agregar(inferior, 2, Boton(_simbolos ? "abc" : "123", (_, _) => { _simbolos = !_simbolos; Construir(); }));
        Agregar(inferior, 3, Boton("espacio", (_, _) => Emitir(" ")));
        Agregar(inferior, 4, Boton("⌫", (_, _) => Emitir(TeclasEspeciales.Borrar)));
        var listo = Boton("Listo", (_, _) => Emitir(TeclasEspeciales.Listo));
        listo.Background = (Brush)FindResource("Brush.Accent");
        listo.Foreground = (Brush)FindResource("Brush.OnAction");
        Agregar(inferior, 5, listo);
        Filas.Children.Add(inferior);
    }

    private static void Agregar(Grid g, int col, UIElement e)
    {
        Grid.SetColumn(e, col);
        g.Children.Add(e);
    }

    private Button Boton(string texto, RoutedEventHandler click, bool activo = false)
    {
        var b = new Button { Content = texto, Style = (Style)FindResource("Button.Tecla"), MinWidth = 52 };
        if (texto.Length > 1) b.FontSize = (double)FindResource("Font.Body");
        if (activo) b.Background = (Brush)FindResource("Brush.Selected");
        b.Click += click;
        return b;
    }

    private void OnCaracter(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string s }) return;
        if (_acento)
        {
            s = s switch
            {
                "a" => "á", "e" => "é", "i" => "í", "o" => "ó", "u" => "ú",
                "A" => "Á", "E" => "É", "I" => "Í", "O" => "Ó", "U" => "Ú",
                _ => s
            };
            _acento = false;
            Construir();
        }
        Emitir(s);
        if (_mayusculas && s.Length == 1 && char.IsLetter(s[0])) { _mayusculas = false; Construir(); } // mayúscula de una sola letra
    }

    private void Emitir(string t) => Tecla?.Invoke(this, t);
}
