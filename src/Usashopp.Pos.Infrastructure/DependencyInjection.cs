using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Infrastructure.Hardware;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Repositories;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Infrastructure.System;

namespace Usashopp.Pos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = ExpandirVariables(config.GetConnectionString("AppDb") ?? "Data Source=pos.db");
        AsegurarCarpetaDeArchivo(connectionString);

        var carpetaRespaldos = ExpandirVariables(config["Infrastructure:CarpetaRespaldos"] ?? "backups");
        var carpetaNube = ExpandirVariables(config["Infrastructure:CarpetaNube"] ?? string.Empty);

        services.Configure<InfrastructureOptions>(o =>
        {
            o.ConnectionString = connectionString;
            o.CarpetaRespaldos = carpetaRespaldos;
            o.CarpetaNube = string.IsNullOrWhiteSpace(carpetaNube) ? null : carpetaNube;
            if (int.TryParse(config["Infrastructure:RetenerUltimos"], out var retener))
                o.RetenerUltimos = retener;
        });

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        // Repositorios y unidad de trabajo (por scope de operación).
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IVarianteRepository, VarianteRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
        services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();
        services.AddScoped<ICompraRepository, CompraRepository>();
        services.AddScoped<IApartadoRepository, ApartadoRepository>();
        services.AddScoped<IConfiguracionTiendaRepository, ConfiguracionTiendaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Servicios del sistema.
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<IBackupService, SqliteBackupService>();

        // Hardware (implementaciones ESC/POS por completar en Fase 4).
        services.AddSingleton<ITicketPrinter, EscPosTicketPrinter>();
        services.AddSingleton<ICashDrawer, EscPosCashDrawer>();

        // Inicializador (migraciones + seed).
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    /// <summary>Expande variables de entorno como %ProgramData% en rutas de configuración.</summary>
    private static string ExpandirVariables(string valor) =>
        Environment.ExpandEnvironmentVariables(valor).Replace('/', Path.DirectorySeparatorChar);

    /// <summary>Crea la carpeta destino del archivo SQLite si no existe.</summary>
    private static void AsegurarCarpetaDeArchivo(string connectionString)
    {
        const string marca = "Data Source=";
        var idx = connectionString.IndexOf(marca, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;

        var ruta = connectionString[(idx + marca.Length)..].Split(';')[0].Trim();
        var carpeta = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrWhiteSpace(carpeta))
            Directory.CreateDirectory(carpeta);
    }
}
