using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>
/// Cambio de talla de punta a punta con los servicios reales y SQLite en archivo (Lote 21):
/// stock de ambas prendas, venta nueva ligada, corte de caja y reportes sin doble conteo.
/// </summary>
public class CambioIntegrationTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "usashopp-cambio-" + Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _sp;
    private Guid _m, _l, _xl;

    public CambioIntegrationTests()
    {
        Directory.CreateDirectory(_carpeta);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AppDb"] = $"Data Source={Path.Combine(_carpeta, "pos.db")}",
            ["Infrastructure:CarpetaRespaldos"] = Path.Combine(_carpeta, "backups"),
            ["Infrastructure:CarpetaImagenes"] = Path.Combine(_carpeta, "img"),
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(config);
        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InicializarAsync().GetAwaiter().GetResult();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = db.Usuarios.First();
        _sp.GetRequiredService<ISesionManager>().IniciarSesion(admin.Id, admin.Nombre, Permisos.Todos);

        var cat = db.Categorias.First();
        var p = new Producto { Nombre = "Playera", CategoriaId = cat.Id };
        VarianteProducto V(string talla, decimal precio)
        {
            var v = new VarianteProducto { Producto = p, Talla = talla, Color = "Negro", Sku = new Sku($"PLY-{talla}"), PrecioVenta = new Dinero(precio), Costo = new Dinero(80m) };
            v.EstablecerStock(5);
            return v;
        }
        var m = V("M", 200m); var l = V("L", 200m); var xl = V("XL", 250m);
        db.AddRange(p, m, l, xl);
        db.SaveChanges();
        (_m, _l, _xl) = (m.Id, l.Id, xl.Id);

        scope.ServiceProvider.GetRequiredService<CajaService>().AbrirAsync(500m).GetAwaiter().GetResult();
    }

    private async Task<ResultadoVentaDto> VenderMAsync()
    {
        using var scope = _sp.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<RegistrarVentaService>().EjecutarAsync(new NuevaVentaDto(
            new[] { new NuevaLineaDto(_m, 1) }, new[] { new NuevoPagoDto(MetodoPago.Efectivo, 200m) }, Imprimir: false, AbrirCajon: false));
        r.Exito.Should().BeTrue(r.Error);
        return r.Valor!;
    }

    private int Stock(Guid id)
    {
        using var scope = _sp.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Variantes.AsNoTracking().First(v => v.Id == id).StockActual;
    }

    [Fact]
    public async Task Cambio_sin_diferencia_mueve_stock_y_no_duplica_ingresos()
    {
        var venta = await VenderMAsync();

        using (var scope = _sp.CreateScope())
        {
            var r = await scope.ServiceProvider.GetRequiredService<CambioService>().EjecutarAsync(new CambioDto(
                venta.VentaId, new[] { new DevolucionItemDto(_m, 1) }, new[] { new NuevaLineaDto(_l, 1) }));
            r.Exito.Should().BeTrue(r.Error);
            r.Valor!.Cobrado.Should().Be(0);
            r.Valor.Devuelto.Should().Be(0);
        }

        Stock(_m).Should().Be(5, "la M vendida regresó");
        Stock(_l).Should().Be(4, "se llevó la L");

        using (var scope = _sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Ventas.Count(v => v.VentaOrigenId == venta.VentaId).Should().Be(1);

            var rep = await scope.ServiceProvider.GetRequiredService<ReportesService>().ObtenerAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            rep.VentasTotal.Should().Be(200m, "el cambio no es un ingreso nuevo");
            rep.NumVentas.Should().Be(1);

            var corte = await scope.ServiceProvider.GetRequiredService<CajaService>().ObtenerCorteAsync();
            corte!.EfectivoEsperado.Should().Be(700m, "fondo 500 + venta 200; el cambio no movió efectivo");
        }
    }

    [Fact]
    public async Task Cambio_con_diferencia_a_cobrar_y_a_devolver()
    {
        var venta = await VenderMAsync();
        using (var scope = _sp.CreateScope())
        {
            var s = scope.ServiceProvider.GetRequiredService<CambioService>();
            // Sin pagar la diferencia no se permite.
            (await s.EjecutarAsync(new CambioDto(venta.VentaId, new[] { new DevolucionItemDto(_m, 1) }, new[] { new NuevaLineaDto(_xl, 1) })))
                .EsFallo.Should().BeTrue();

            var r = await s.EjecutarAsync(new CambioDto(venta.VentaId, new[] { new DevolucionItemDto(_m, 1) },
                new[] { new NuevaLineaDto(_xl, 1) }, new[] { new NuevoPagoDto(MetodoPago.Efectivo, 50m) }));
            r.Exito.Should().BeTrue(r.Error);
            r.Valor!.Cobrado.Should().Be(50m);
        }

        // Segunda venta y cambio por algo más barato con devolución en efectivo.
        var venta2 = await VenderMAsync();
        using (var scope = _sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var l = db.Variantes.First(v => v.Id == _l);
            l.PrecioVenta = new Dinero(150m);
            await db.SaveChangesAsync();
        }
        using (var scope = _sp.CreateScope())
        {
            var r = await scope.ServiceProvider.GetRequiredService<CambioService>().EjecutarAsync(new CambioDto(
                venta2.VentaId, new[] { new DevolucionItemDto(_m, 1) }, new[] { new NuevaLineaDto(_l, 1) }));
            r.Exito.Should().BeTrue(r.Error);
            r.Valor!.Devuelto.Should().Be(50m);

            var corte = await scope.ServiceProvider.GetRequiredService<CajaService>().ObtenerCorteAsync();
            // fondo 500 + ventas 200 + 200 + diferencia cobrada 50 − devolución 50
            corte!.EfectivoEsperado.Should().Be(900m);
        }
    }

    public void Dispose()
    {
        _sp.Dispose();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_carpeta, true); } catch { /* temporal */ }
    }
}
