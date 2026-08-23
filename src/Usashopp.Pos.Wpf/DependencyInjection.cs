using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Wpf.Common;
using Usashopp.Pos.Wpf.Features.Apartados;
using Usashopp.Pos.Wpf.Features.Clientes;
using Usashopp.Pos.Wpf.Features.Compras;
using Usashopp.Pos.Wpf.Features.Configuracion;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Login;
using Usashopp.Pos.Wpf.Features.Pos;
using Usashopp.Pos.Wpf.Features.Proveedores;
using Usashopp.Pos.Wpf.Features.Reportes;
using Usashopp.Pos.Wpf.Features.Shell;
using Usashopp.Pos.Wpf.Features.Usuarios;
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

        // Login (se muestra antes de la ventana principal).
        services.AddTransient<LoginWindow>();
        services.AddTransient<LoginViewModel>();

        // Los ViewModels de contenido son transitorios (nueva instancia por navegación).
        services.AddTransient<PosViewModel>();
        services.AddTransient<InventarioViewModel>();
        services.AddTransient<VentasViewModel>();
        services.AddTransient<ClientesViewModel>();
        services.AddTransient<ProveedoresViewModel>();
        services.AddTransient<ComprasViewModel>();
        services.AddTransient<ApartadosViewModel>();
        services.AddTransient<ReportesViewModel>();
        services.AddTransient<ConfiguracionViewModel>();
        services.AddTransient<UsuariosViewModel>();

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
        services.AddTransient<ApartadoEditorViewModel>();
        services.AddTransient<ApartadoEditorWindow>();
        services.AddTransient<AbonoViewModel>();
        services.AddTransient<AbonoWindow>();
        services.AddTransient<UsuarioEditorViewModel>();
        services.AddTransient<UsuarioEditorWindow>();

        return services;
    }
}
