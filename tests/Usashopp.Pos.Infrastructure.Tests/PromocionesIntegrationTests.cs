using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Promociones;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>Promoción 2×1 de punta a punta: carrito = venta registrada, ticket y devolución prorrateada (Lote 22).</summary>
public class PromocionesIntegrationTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "usashopp-promo-" + Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _sp;
    private Guid _negra, _blanca, _categoria, _producto;

    public PromocionesIntegrationTests()
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
        VarianteProducto V(string color, decimal precio)
        {
            var v = new VarianteProducto { Producto = p, Talla = "M", Color = color, Sku = new Sku($"PLY-{color}"), PrecioVenta = new Dinero(precio) };
            v.EstablecerStock(10);
            return v;
        }
        var negra = V("Negro", 300m); var blanca = V("Blanco", 200m);
        db.AddRange(p, negra, blanca);
        db.SaveChanges();
        (_negra, _blanca, _categoria, _producto) = (negra.Id, blanca.Id, cat.Id, p.Id);

        scope.ServiceProvider.GetRequiredService<CajaService>().AbrirAsync(0m).GetAwaiter().GetResult();
        scope.ServiceProvider.GetRequiredService<PromocionService>().GuardarAsync(new PromocionDto(
            Guid.Empty, "2x1 playeras", TipoPromocion.NxM, AlcancePromocion.Producto, _producto, 0, 2, 1,
            null, null, 0, true, 0, false)).GetAwaiter().GetResult().Exito.Should().BeTrue();
    }

    [Fact]
    public async Task Dos_por_uno_se_cobra_igual_que_en_pantalla_y_la_devolucion_se_prorratea()
    {
        using var scope = _sp.CreateScope();
        var promos = await scope.ServiceProvider.GetRequiredService<PromocionService>().ListarVigentesAsync();

        // Lo que ve el cajero
        var pantalla = CalculadoraTotales.Calcular(new[]
        {
            new LineaCalculo(_negra, 1, 300m, ProductoId: _producto, CategoriaId: _categoria),
            new LineaCalculo(_blanca, 1, 200m, ProductoId: _producto, CategoriaId: _categoria)
        }, promociones: promos);
        pantalla.Total.Should().Be(300m);
        pantalla.Lineas.Should().OnlyContain(l => l.Promocion == "2x1 playeras");

        // Lo que se registra
        var r = await scope.ServiceProvider.GetRequiredService<RegistrarVentaService>().EjecutarAsync(new NuevaVentaDto(
            new[] { new NuevaLineaDto(_negra, 1), new NuevaLineaDto(_blanca, 1) },
            new[] { new NuevoPagoDto(MetodoPago.Efectivo, 300m) }, Imprimir: false, AbrirCajon: false));
        r.Exito.Should().BeTrue(r.Error);
        r.Valor!.Total.Should().Be(pantalla.Total);

        var detalle = await scope.ServiceProvider.GetRequiredService<ConsultarVentasService>().ObtenerDetalleAsync(r.Valor.VentaId);
        detalle!.Lineas.Should().OnlyContain(l => l.Promocion == "2x1 playeras");

        // Devolver la blanca: se reembolsa lo que realmente se pagó por ella (prorrateado), no $200.
        var reembolso = await scope.ServiceProvider.GetRequiredService<DevolucionService>()
            .CalcularReembolsoAsync(r.Valor.VentaId, new[] { new DevolucionItemDto(_blanca, 1) });
        reembolso.Should().Be(120m, "el descuento de $200 se repartió 60/40 entre $300 y $200");
    }

    public void Dispose()
    {
        _sp.Dispose();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_carpeta, true); } catch { /* temporal */ }
    }
}
