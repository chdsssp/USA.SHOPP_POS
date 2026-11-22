using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Tablas usables con el dedo, donde el doble clic falla seguido:
/// <list type="bullet">
/// <item><c>TablaTactil.AbrirFila</c> en un botón dentro de una fila: selecciona esa fila y
/// ejecuta el comando (p. ej. el botón "Editar" visible en cada renglón).</item>
/// <item><c>TablaTactil.ComandoAlTocar</c> en el DataGrid: tocar con el dedo una fila que YA estaba
/// seleccionada ejecuta el comando (equivale al doble clic del mouse).</item>
/// </list>
/// </summary>
public static class TablaTactil
{
    // ---------- Botón dentro de la fila ----------

    public static readonly DependencyProperty AbrirFilaProperty = DependencyProperty.RegisterAttached(
        "AbrirFila", typeof(ICommand), typeof(TablaTactil), new PropertyMetadata(null, OnAbrirFilaChanged));

    public static ICommand? GetAbrirFila(DependencyObject d) => (ICommand?)d.GetValue(AbrirFilaProperty);
    public static void SetAbrirFila(DependencyObject d, ICommand? value) => d.SetValue(AbrirFilaProperty, value);

    private static void OnAbrirFilaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase b) return;
        b.Click -= OnBotonClick;
        if (e.NewValue is not null) b.Click += OnBotonClick;
    }

    private static void OnBotonClick(object sender, RoutedEventArgs e)
    {
        var boton = (ButtonBase)sender;
        if (BuscarAncestro<DataGrid>(boton) is { } grid) grid.SelectedItem = boton.DataContext;
        var cmd = GetAbrirFila(boton);
        if (cmd?.CanExecute(null) == true) cmd.Execute(null);
        e.Handled = true;
    }

    // ---------- Tocar una fila ya seleccionada ----------

    public static readonly DependencyProperty ComandoAlTocarProperty = DependencyProperty.RegisterAttached(
        "ComandoAlTocar", typeof(ICommand), typeof(TablaTactil), new PropertyMetadata(null, OnComandoAlTocarChanged));

    public static ICommand? GetComandoAlTocar(DependencyObject d) => (ICommand?)d.GetValue(ComandoAlTocarProperty);
    public static void SetComandoAlTocar(DependencyObject d, ICommand? value) => d.SetValue(ComandoAlTocarProperty, value);

    private static readonly DependencyProperty SeleccionPreviaProperty = DependencyProperty.RegisterAttached(
        "SeleccionPrevia", typeof(object), typeof(TablaTactil));

    private static void OnComandoAlTocarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid g) return;
        g.PreviewTouchDown -= OnTouchDown;
        g.TouchUp -= OnTouchUp;
        if (e.NewValue is null) return;
        g.PreviewTouchDown += OnTouchDown;
        g.TouchUp += OnTouchUp;
    }

    private static void OnTouchDown(object sender, TouchEventArgs e) =>
        ((DataGrid)sender).SetValue(SeleccionPreviaProperty, ((DataGrid)sender).SelectedItem);

    private static void OnTouchUp(object sender, TouchEventArgs e)
    {
        var g = (DataGrid)sender;
        var fila = BuscarAncestro<DataGridRow>(e.OriginalSource as DependencyObject);
        if (fila is null || fila.Item is null) return;
        if (!ReferenceEquals(fila.Item, g.GetValue(SeleccionPreviaProperty))) return; // primer toque = seleccionar
        var cmd = GetComandoAlTocar(g);
        if (cmd?.CanExecute(null) == true) cmd.Execute(null);
    }

    private static T? BuscarAncestro<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d is not null && d is not T)
            d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        return d as T;
    }
}
