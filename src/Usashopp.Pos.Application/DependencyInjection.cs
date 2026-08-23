using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Apartados;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Proveedores;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Application.Ventas;

namespace Usashopp.Pos.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y validadores de la capa Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Casos de uso (servicios de aplicación)
        services.AddScoped<BuscarProductosService>();
        services.AddScoped<RegistrarVentaService>();
        services.AddScoped<ConsultarVentasService>();
        services.AddScoped<CancelarVentaService>();
        services.AddScoped<ReportesService>();
        services.AddScoped<ConfiguracionService>();
        services.AddScoped<CategoriaService>();
        services.AddScoped<ProductoService>();
        services.AddScoped<InventarioService>();
        services.AddScoped<CajaService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<ProveedorService>();
        services.AddScoped<RegistrarCompraService>();
        services.AddScoped<ConsultarComprasService>();
        services.AddScoped<ApartadoService>();

        return services;
    }
}
