using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Wpf.Common;
using Usashopp.Pos.Wpf.Features.Clientes;
using Usashopp.Pos.Wpf.Features.Compras;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;
using Usashopp.Pos.Wpf.Features.Proveedores;
using Usashopp.Pos.Wpf.Features.Shell;
using Usashopp.Pos.Wpf.Features.Ventas;

namespace Usashopp.Pos.Wpf;

public static class DependencyInjection
{
    /// <summary>Registra ventanas, ViewModels y servicios de la capa de presentación.</summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<IDialogService, DialogService>();

        // Los ViewModels de contenido son transitorios (nueva instancia por navegación).
        services.AddTransient<PosViewModel>();
        services.AddTransient<InventarioViewModel>();
        services.AddTransient<VentasViewModel>();
        services.AddTransient<ClientesViewModel>();
        services.AddTransient<ProveedoresViewModel>();
        services.AddTransient<ComprasViewModel>();

        // Diálogos (ventana + su ViewModel).
        services.AddTransient<ProductoEditorViewModel>();
        services.AddTransient<ProductoEditorWindow>();
        services.AddTransient<AjusteStockViewModel>();
        services.AddTransient<AjusteStockWindow>();
        services.AddTransient<CobroViewModel>();
        services.AddTransient<CobroWindow>();
        services.AddTransient<AbrirCajaViewModel>();
        services.AddTransient<AbrirCajaWindow>();
        services.AddTransient<CorteCajaViewModel>();
        services.AddTransient<CorteCajaWindow>();
        services.AddTransient<ClienteEditorViewModel>();
        services.AddTransient<ClienteEditorWindow>();
        services.AddTransient<ProveedorEditorViewModel>();
        services.AddTransient<ProveedorEditorWindow>();
        services.AddTransient<CompraEditorViewModel>();
        services.AddTransient<CompraEditorWindow>();

        return services;
    }
}
