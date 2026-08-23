using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;

namespace Usashopp.Pos.Wpf.Common;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services) => _services = services;

    public bool MostrarEditorProducto()
    {
        var ventana = _services.GetRequiredService<ProductoEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarAjusteStock(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<AjusteStockWindow>();
        if (ventana.DataContext is AjusteStockViewModel vm)
            vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public CobroResultado? MostrarCobro(decimal total)
    {
        var ventana = _services.GetRequiredService<CobroWindow>();
        if (ventana.DataContext is CobroViewModel vm)
        {
            vm.Inicializar(total);
            ventana.Owner = System.Windows.Application.Current.MainWindow;
            return ventana.ShowDialog() == true ? vm.Resultado : null;
        }
        return null;
    }

    public decimal? MostrarAbrirCaja()
    {
        var ventana = _services.GetRequiredService<AbrirCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        if (ventana.ShowDialog() == true && ventana.DataContext is AbrirCajaViewModel vm)
            return vm.FondoInicial;
        return null;
    }

    public bool MostrarCorteCaja()
    {
        var ventana = _services.GetRequiredService<CorteCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public void Mensaje(string texto, string titulo = "USASHOPP POS") =>
        MessageBox.Show(texto, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
}
