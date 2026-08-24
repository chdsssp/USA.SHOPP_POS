using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Usashopp.Pos.Application;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Infrastructure;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Wpf.Features.Login;
using Usashopp.Pos.Wpf.Features.Shell;

namespace Usashopp.Pos.Wpf;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;
    private DispatcherTimer? _timerRespaldo;

    public App()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        ConfigurarLogging();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddPresentation();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Excepción no controlada en la UI");
            MessageBox.Show("Ocurrió un error inesperado. El detalle se registró en el log.",
                "USASHOPP POS", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        await _host.StartAsync();

        // Migraciones + datos semilla dentro de un scope.
        using (var scope = _host.Services.CreateScope())
        {
            var init = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await init.InicializarAsync();
        }

        // No cerrar la app entre el login y la ventana principal.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var login = _host.Services.GetRequiredService<LoginWindow>();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var ventana = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = ventana;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        ventana.Show();

        ConfigurarRespaldoAutomatico();
    }

    /// <summary>Respaldo periódico de la base según Infrastructure:CadaHoras (0 = desactivado).</summary>
    private void ConfigurarRespaldoAutomatico()
    {
        var config = _host.Services.GetRequiredService<IConfiguration>();
        if (!int.TryParse(config["Infrastructure:CadaHoras"], out var horas) || horas <= 0)
            return;

        _timerRespaldo = new DispatcherTimer { Interval = TimeSpan.FromHours(horas) };
        _timerRespaldo.Tick += async (_, _) =>
        {
            try
            {
                using var scope = _host.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IBackupService>().CrearRespaldoAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Falló el respaldo automático programado");
            }
        };
        _timerRespaldo.Start();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigurarLogging()
    {
        var carpetaLogs = Environment
            .ExpandEnvironmentVariables("%ProgramData%/USASHOPP POS/logs")
            .Replace('/', Path.DirectorySeparatorChar);
        Directory.CreateDirectory(carpetaLogs);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(carpetaLogs, "pos-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
