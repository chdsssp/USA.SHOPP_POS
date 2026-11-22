using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Usashopp.Pos.Application;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Infrastructure;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Wpf;
using Usashopp.Pos.Wpf.Features.Shell;
using Xunit;
using Xunit.Abstractions;

namespace Usashopp.Pos.Wpf.Tests;

/// <summary>
/// Prueba de humo de TODA la UI: con DI real y una BD SQLite temporal, crea cada vista y cada
/// ventana, aplica plantillas (Measure/Arrange) y deja correr la carga asíncrona de su ViewModel.
/// Detecta errores de XAML (recursos inexistentes, estilos mal formados) y excepciones al cargar
/// que de otro modo solo aparecerían al abrir la pantalla en el equipo.
/// </summary>
public class VistasSmokeTests
{
    private readonly ITestOutputHelper _out;
    public VistasSmokeTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Todas_las_vistas_y_ventanas_cargan_sin_errores()
    {
        var errores = new List<string>();
        var bindings = new StringBuilder();
        var hilo = new Thread(() => Ejecutar(errores, bindings));
        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        hilo.Join();

        if (bindings.Length > 0) _out.WriteLine("Errores de binding:\n" + bindings);
        foreach (var e in errores) _out.WriteLine(e);
        errores.Should().BeEmpty();
    }

    private static void Ejecutar(List<string> errores, StringBuilder bindings)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "usashopp-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        try
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Application.LoadComponent(
                new Uri("/USASHOPP POS;component/Themes/Theme.xaml", UriKind.Relative)));

            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingListener(bindings));
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            Dispatcher.CurrentDispatcher.UnhandledException += (_, e) =>
            {
                errores.Add("Excepción no controlada: " + e.Exception);
                e.Handled = true;
            };

            using var host = CrearHost(carpeta);
            var sp = host.Services;
            Inicializar(sp);

            var asm = typeof(MainWindow).Assembly;
            var tipos = asm.GetTypes()
                .Where(t => !t.IsAbstract && t.Namespace?.Contains(".Features.") == true &&
                            (typeof(Window).IsAssignableFrom(t) || typeof(UserControl).IsAssignableFrom(t)))
                .OrderBy(t => t.FullName)
                .ToList();

            foreach (var tipo in tipos)
            {
                try
                {
                    var vista = Crear(sp, tipo);
                    if (vista is UserControl uc && uc.DataContext is null)
                        uc.DataContext = CrearViewModelPorConvencion(sp, tipo);

                    // Las ventanas se prueban tal como las abre la app: envueltas en la hoja táctil.
                    if (vista is Window wh) Usashopp.Pos.Wpf.Common.HojaTactil.Preparar(wh);
                    var raiz = vista is Window w ? w.Content as FrameworkElement : vista as FrameworkElement;
                    if (raiz is not null)
                    {
                        raiz.Measure(new Size(1366, 768));
                        raiz.Arrange(new Rect(0, 0, 1366, 768));
                        raiz.UpdateLayout();
                    }
                    Bombear(TimeSpan.FromMilliseconds(400)); // deja correr la carga async del ViewModel
                    if (vista is Window win) win.Close();
                }
                catch (Exception ex)
                {
                    errores.Add($"{tipo.Name}: {ex.GetType().Name}: {ex.Message}" +
                                (ex.InnerException is { } ie ? $" → {ie.GetType().Name}: {ie.Message}" : ""));
                }
            }

            app.Shutdown();
        }
        catch (Exception ex)
        {
            errores.Add("Fallo general: " + ex);
        }
        finally
        {
            SqliteLimpiar();
            try { Directory.Delete(carpeta, true); } catch { /* archivos aún bloqueados: no importa */ }
        }
    }

    private static IHost CrearHost(string carpeta)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AppDb"] = $"Data Source={Path.Combine(carpeta, "pos_ui.db")}",
            ["Infrastructure:CarpetaRespaldos"] = Path.Combine(carpeta, "backups"),
            ["Infrastructure:CarpetaImagenes"] = Path.Combine(carpeta, "imagenes"),
            ["Infrastructure:CarpetaTerminal"] = carpeta,
        });
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddPresentation();
        return builder.Build();
    }

    private static void Inicializar(IServiceProvider sp)
    {
        using (var scope = sp.CreateScope())
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InicializarAsync().GetAwaiter().GetResult();

        using var s2 = sp.CreateScope();
        var db = s2.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = db.Usuarios.AsNoTracking().First();
        sp.GetRequiredService<ISesionManager>().IniciarSesion(admin.Id, admin.Nombre, Permisos.Todos);
    }

    /// <summary>Crea el tipo resolviendo su constructor con DI; lo que DI no sabe crear va "vacío".</summary>
    private static object Crear(IServiceProvider sp, Type tipo)
    {
        var ctor = tipo.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => Resolver(sp, p.ParameterType)).ToArray();
        return ctor.Invoke(args);
    }

    private static object? Resolver(IServiceProvider sp, Type t)
    {
        var s = sp.GetService(t);
        if (s is not null) return s;
        try { return ActivatorUtilities.CreateInstance(sp, t); }
        catch
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (t == typeof(string)) return string.Empty;
            return RuntimeHelpers.GetUninitializedObject(t);
        }
    }

    private static object? CrearViewModelPorConvencion(IServiceProvider sp, Type vista)
    {
        var nombreVm = vista.FullName!.EndsWith("View") ? vista.FullName + "Model" : null;
        var vm = nombreVm is null ? null : vista.Assembly.GetType(nombreVm);
        return vm is null ? null : Resolver(sp, vm);
    }

    private static void Bombear(TimeSpan duracion)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(duracion, DispatcherPriority.Background, (_, _) => frame.Continue = false,
            Dispatcher.CurrentDispatcher);
        timer.Start();
        Dispatcher.PushFrame(frame);
        timer.Stop();
    }

    private static void SqliteLimpiar() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

    private sealed class BindingListener : TraceListener
    {
        private readonly StringBuilder _sb;
        public BindingListener(StringBuilder sb) => _sb = sb;
        public override void Write(string? message) { }
        public override void WriteLine(string? message) => _sb.AppendLine(message);
    }
}
