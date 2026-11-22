using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Usashopp.Pos.Application.Productos.Dtos;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Tabla talla × color de un producto para vender en 2 toques: filas = colores, columnas = tallas
/// en orden lógico; cada casilla muestra la existencia (gris = agotado, ámbar = poca existencia).
/// "Agregar 1 y cerrar" (por defecto) o "Agregar varias" para sumar piezas antes de cerrar.
/// </summary>
public partial class MatrizVariantesWindow : Window
{
    private readonly Dictionary<Guid, (ProductoBusquedaDto Variante, int Cantidad)> _elegidas = new();
    private readonly Dictionary<Guid, TextBlock> _contadores = new();
    private bool _permitirAgotadas;

    /// <summary>Variantes elegidas con su cantidad (vacío si se canceló).</summary>
    public IReadOnlyList<(ProductoBusquedaDto Variante, int Cantidad)> Seleccion => _elegidas.Values.ToList();

    public MatrizVariantesWindow() => InitializeComponent();

    public void Inicializar(MatrizVariantesDto matriz, bool permitirAgotadas)
    {
        DataContext = matriz;
        _permitirAgotadas = permitirAgotadas;
        Construir(matriz);
    }

    private void Construir(MatrizVariantesDto m)
    {
        Tabla.Children.Clear();
        Tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        foreach (var _ in m.Tallas) Tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        foreach (var _ in m.Colores) Tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var c = 0; c < m.Tallas.Count; c++)
            Poner(Encabezado(m.Tallas[c], HorizontalAlignment.Center), 0, c + 1);

        for (var f = 0; f < m.Colores.Count; f++)
        {
            Poner(Encabezado(m.Colores[f], HorizontalAlignment.Left), f + 1, 0);
            for (var c = 0; c < m.Tallas.Count; c++)
                Poner(Celda(m.Filas[f][c]), f + 1, c + 1);
        }
    }

    private void Poner(UIElement e, int fila, int col)
    {
        Grid.SetRow(e, fila);
        Grid.SetColumn(e, col);
        Tabla.Children.Add(e);
    }

    private TextBlock Encabezado(string texto, HorizontalAlignment alineacion) => new()
    {
        Text = texto,
        FontWeight = FontWeights.SemiBold,
        Foreground = (Brush)FindResource("Brush.Text.Secondary"),
        HorizontalAlignment = alineacion,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(8, 6, 12, 6)
    };

    private UIElement Celda(CeldaVarianteDto celda)
    {
        var alto = (double)FindResource("TouchTarget.Grande");
        if (celda.Variante is not { } v)
            return new Border { Width = alto + 16, Height = alto, Margin = new Thickness(4) };

        var stock = new TextBlock
        {
            Text = v.Stock <= 0 ? "Agotado" : v.Stock.ToString(),
            FontSize = (double)FindResource(v.Stock <= 0 ? "Font.Caption" : "Font.Subtitle"),
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var contador = new TextBlock
        {
            FontSize = (double)FindResource("Font.Caption"),
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("Brush.Accent"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        _contadores[v.VarianteId] = contador;

        var boton = new Button
        {
            Style = (Style)FindResource("Button.Secondary"),
            Width = alto + 16,
            MinHeight = alto,
            Margin = new Thickness(4),
            Padding = new Thickness(2),
            ToolTip = $"{v.Descripcion} · {v.Precio:C2} · SKU {v.Sku}",
            Content = new StackPanel { Children = { stock, contador } },
            IsEnabled = v.Stock > 0 || _permitirAgotadas
        };
        if (v.Stock <= 0) boton.Foreground = (Brush)FindResource("Brush.Text.Disabled");
        else if (celda.BajoStock) boton.Background = (Brush)FindResource("Brush.Warning.Bg");
        boton.Click += (_, _) => Elegir(v);
        return boton;
    }

    private void Elegir(ProductoBusquedaDto v)
    {
        var actual = _elegidas.TryGetValue(v.VarianteId, out var e) ? e.Cantidad : 0;
        _elegidas[v.VarianteId] = (v, actual + 1);

        if (UnaPieza.IsChecked == true)
        {
            DialogResult = true;
            Close();
            return;
        }

        var c = _contadores[v.VarianteId];
        c.Text = $"+{actual + 1}";
        c.Visibility = Visibility.Visible;
        var total = _elegidas.Values.Sum(x => x.Cantidad);
        Agregar.IsEnabled = true;
        Agregar.Content = $"Agregar al carrito ({total})";
    }

    private void OnAgregar(object sender, RoutedEventArgs e)
    {
        DialogResult = _elegidas.Count > 0;
        Close();
    }

    private void OnCancelar(object sender, RoutedEventArgs e)
    {
        _elegidas.Clear();
        DialogResult = false;
        Close();
    }
}
