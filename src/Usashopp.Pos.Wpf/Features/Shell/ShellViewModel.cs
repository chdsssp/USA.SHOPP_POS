using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Wpf.Common;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>
/// ViewModel del shell: barra superior, navegación lateral y contenido activo.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private object? _contenidoActual;

    [ObservableProperty]
    private string _nombreTienda = "test_tienda";

    [ObservableProperty]
    private string _usuario = "test_usuario";

    [ObservableProperty]
    private string _estadoCaja = "Caja abierta";

    public ObservableCollection<MenuItemViewModel> Menu { get; } = new()
    {
        new("pos", "Punto de venta"),
        new("inventario", "Inventario"),
        new("ventas", "Ventas"),
        new("apartados", "Apartados"),
        new("compras", "Compras"),
        new("clientes", "Clientes"),
        new("proveedores", "Proveedores"),
        new("reportes", "Reportes"),
        new("configuracion", "Configuración"),
    };

    public ShellViewModel(IServiceProvider services)
    {
        _services = services;
        Navegar(Menu[0]); // Inicia en Punto de venta.
    }

    [RelayCommand]
    private void Navegar(MenuItemViewModel item)
    {
        foreach (var m in Menu)
            m.Activo = ReferenceEquals(m, item);

        ContenidoActual = item.Clave switch
        {
            "pos" => _services.GetRequiredService<PosViewModel>(),
            "inventario" => _services.GetRequiredService<InventarioViewModel>(),
            _ => new PlaceholderViewModel(item.Titulo)
        };
    }
}
