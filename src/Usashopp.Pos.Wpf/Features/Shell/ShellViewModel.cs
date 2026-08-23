using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Wpf.Common;
using Usashopp.Pos.Wpf.Features.Clientes;
using Usashopp.Pos.Wpf.Features.Compras;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;
using Usashopp.Pos.Wpf.Features.Proveedores;
using Usashopp.Pos.Wpf.Features.Ventas;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>
/// ViewModel del shell: barra superior, navegación lateral y contenido activo.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private object? _contenidoActual;
    [ObservableProperty] private string _nombreTienda = "test_tienda";
    [ObservableProperty] private string _usuario = "test_usuario";
    [ObservableProperty] private bool _cajaAbierta;
    [ObservableProperty] private string _cajaTexto = "Caja cerrada";

    public ObservableCollection<MenuItemViewModel> Menu { get; } = new()
    {
        new("pos",           "Punto de venta", "M3,4 H21 V15 H3 Z M9,19 H15 M12,15 V19"),
        new("inventario",    "Inventario",     "M4,7 L12,3 L20,7 L12,11 Z M4,7 V17 L12,21 M20,7 V17 L12,21 M12,11 V21"),
        new("ventas",        "Ventas",         "M6,2 H18 V22 L15,20 L12,22 L9,20 L6,22 Z M9,7 H15 M9,11 H15 M9,15 H14"),
        new("apartados",     "Apartados",      "M6,3 H18 V21 L12,16 L6,21 Z"),
        new("compras",       "Compras",        "M3,4 H5 L7,15 H18 L20,7 H6 M8,18 H10 V20 H8 Z M16,18 H18 V20 H16 Z"),
        new("clientes",      "Clientes",       "M12,4 L15,7 L12,10 L9,7 Z M5,20 L7,14 H17 L19,20 Z"),
        new("proveedores",   "Proveedores",    "M3,7 H14 V16 H3 Z M14,10 H18 L21,13 V16 H14 Z M6,18 H8 V20 H6 Z M16,18 H18 V20 H16 Z"),
        new("reportes",      "Reportes",       "M4,4 V20 H20 M8,16 V12 M12,16 V8 M16,16 V14"),
        new("configuracion", "Configuración",  "M4,7 H20 M4,12 H20 M4,17 H20 M8,5 V9 M14,10 V14 M6,15 V19"),
    };

    public ShellViewModel(IServiceProvider services, IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _services = services;
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;

        WeakReferenceMessenger.Default.Register<CajaEstadoCambiadoMessage>(this, (_, _) => _ = RefrescarCajaAsync());

        Navegar(Menu[0]); // Inicia en Punto de venta.
        _ = RefrescarCajaAsync();
    }

    [RelayCommand]
    private void CorteCaja()
    {
        if (_dialogos.MostrarCorteCaja())
            _ = RefrescarCajaAsync();
    }

    private async Task RefrescarCajaAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
        var sesion = await caja.ObtenerAbiertaAsync();
        CajaAbierta = sesion is not null;
        CajaTexto = sesion is null ? "Caja cerrada" : $"Caja abierta · Fondo {sesion.FondoInicial:C0}";
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
            "ventas" => _services.GetRequiredService<VentasViewModel>(),
            "clientes" => _services.GetRequiredService<ClientesViewModel>(),
            "proveedores" => _services.GetRequiredService<ProveedoresViewModel>(),
            "compras" => _services.GetRequiredService<ComprasViewModel>(),
            _ => new PlaceholderViewModel(item.Titulo)
        };
    }
}
