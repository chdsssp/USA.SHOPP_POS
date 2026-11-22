using System.IO;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Usashopp.Pos.Application;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Wpf.Common;
using Usashopp.Pos.Wpf.Features.Pos;
using Xunit;

namespace Usashopp.Pos.Wpf.Tests;

/// <summary>
/// Pruebas del ViewModel del POS con servicios reales (SQLite temporal) y diálogos falsos:
/// el flujo que más usa el cajero (Lote 24).
/// </summary>
public class PosViewModelTests
{
    [Fact]
    public void Escanear_cobrar_y_ver_el_cambio_en_grande() => EnHiloUi(async ctx =>
    {
        var vm = await ctx.CrearPosAsync();

        await vm.ProcesarCodigoEscaneadoAsync("PLY-M");
        await vm.ProcesarCodigoEscaneadoAsync("PLY-M");
        vm.Carrito.Should().ContainSingle().Which.Cantidad.Should().Be(2);
        vm.Total.Should().Be(398m);

        ctx.Dialogos.MostrarCobro(398m, Arg.Any<bool>(), Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(new CobroResultado(new[] { new NuevoPagoDto(MetodoPago.Efectivo, 500m) }, 102m));
        await vm.CobrarCommand.ExecuteAsync(null);

        vm.MostrandoResultado.Should().BeTrue();
        vm.UltimoResultado!.Cambio.Should().Be(102m);
        vm.Carrito.Should().BeEmpty();

        // Escanear lo siguiente cierra la pantalla de resultado y empieza otra venta.
        await vm.ProcesarCodigoEscaneadoAsync("PLY-M");
        vm.MostrandoResultado.Should().BeFalse();
        vm.Carrito.Should().ContainSingle();
    });

    [Fact]
    public void Poner_en_espera_y_recuperar_la_venta() => EnHiloUi(async ctx =>
    {
        var vm = await ctx.CrearPosAsync();
        await vm.ProcesarCodigoEscaneadoAsync("PLY-M");
        vm.Notas = "La recoge su hermana";

        await vm.SuspenderVentaCommand.ExecuteAsync(null);
        vm.Carrito.Should().BeEmpty();
        vm.EnEsperaCount.Should().Be(1);

        // Otro ViewModel (como si se reiniciara la app): la venta sigue guardada.
        var vm2 = await ctx.CrearPosAsync();
        vm2.EnEsperaCount.Should().Be(1);
        using (var scope = ctx.Servicios.CreateScope())
        {
            var espera = (await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().ListarAsync()).Single();
            ctx.Dialogos.MostrarVentasEnEspera().Returns(espera);
        }
        await vm2.RecuperarVentaCommand.ExecuteAsync(null);

        vm2.Carrito.Should().ContainSingle();
        vm2.Notas.Should().Be("La recoge su hermana");
        vm2.EnEsperaCount.Should().Be(0);
    });

    // ---------------------------------------------------------------------------------------

    private sealed class Contexto : IDisposable
    {
        private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "usashopp-posvm-" + Guid.NewGuid().ToString("N"));
        private readonly IHost _host;
        public IDialogService Dialogos { get; } = Substitute.For<IDialogService>();
        public IServiceProvider Servicios => _host.Services;

        public Contexto()
        {
            Directory.CreateDirectory(_carpeta);
            var b = Host.CreateApplicationBuilder();
            b.Logging.ClearProviders();
            b.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = $"Data Source={Path.Combine(_carpeta, "pos.db")}",
                ["Infrastructure:CarpetaRespaldos"] = Path.Combine(_carpeta, "backups"),
                ["Infrastructure:CarpetaImagenes"] = Path.Combine(_carpeta, "img"),
                ["Infrastructure:CarpetaTerminal"] = _carpeta,
            });
            b.Services.AddApplication();
            b.Services.AddInfrastructure(b.Configuration);
            b.Services.AddPresentation();
            b.Services.AddSingleton(Dialogos); // diálogos falsos (último registro gana)
            _host = b.Build();
        }

        public async Task PrepararAsync()
        {
            using var scope = Servicios.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InicializarAsync();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = db.Usuarios.First();
            Servicios.GetRequiredService<ISesionManager>().IniciarSesion(admin.Id, admin.Nombre, Permisos.Todos);

            var p = new Producto { Nombre = "Playera", CategoriaId = db.Categorias.First().Id };
            var v = new VarianteProducto { Producto = p, Talla = "M", Color = "Negro", Sku = new Sku("PLY-M"), PrecioVenta = new Dinero(199m) };
            v.EstablecerStock(10);
            db.AddRange(p, v);
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<CajaService>().AbrirAsync(0m);
        }

        public async Task<PosViewModel> CrearPosAsync()
        {
            var vm = Servicios.GetRequiredService<PosViewModel>();
            await Esperar(TimeSpan.FromMilliseconds(600)); // carga inicial (caja, grid, borrador…)
            return vm;
        }

        public void Dispose()
        {
            _host.Dispose();
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_carpeta, true); } catch { /* temporal */ }
        }
    }

    private static Task Esperar(TimeSpan t) => Task.Delay(t);

    /// <summary>Ejecuta la prueba en un hilo STA con Dispatcher (como la UI real) y propaga fallos.</summary>
    private static void EnHiloUi(Func<Contexto, Task> prueba)
    {
        Exception? error = null;
        var hilo = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            using var ctx = new Contexto();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await ctx.PrepararAsync();
                    await prueba(ctx);
                }
                catch (Exception ex) { error = ex; }
                finally { frame.Continue = false; }
            });
            Dispatcher.PushFrame(frame);
        });
        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        hilo.Join();
        if (error is not null) throw new Xunit.Sdk.XunitException(error.ToString());
    }
}
