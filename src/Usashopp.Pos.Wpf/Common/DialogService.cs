using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Wpf.Features.Clientes;
using Usashopp.Pos.Wpf.Features.Compras;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;
using Usashopp.Pos.Wpf.Features.Proveedores;

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

    public bool MostrarEditorCliente(ClienteDto? cliente)
    {
        var ventana = _services.GetRequiredService<ClienteEditorWindow>();
        if (ventana.DataContext is ClienteEditorViewModel vm) vm.Inicializar(cliente);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorProveedor(ProveedorDto? proveedor)
    {
        var ventana = _services.GetRequiredService<ProveedorEditorWindow>();
        if (ventana.DataContext is ProveedorEditorViewModel vm) vm.Inicializar(proveedor);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorCompra()
    {
        var ventana = _services.GetRequiredService<CompraEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public void Mensaje(string texto, string titulo = "USASHOPP POS") =>
        MessageBox.Show(texto, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
}
