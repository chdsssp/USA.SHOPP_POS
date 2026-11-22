using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>Las ventas en espera sobreviven a "reiniciar" (nuevo contexto) y se revalidan al recuperar.</summary>
public class VentasEnEsperaIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly Guid _usuarioId = Guid.NewGuid();
    private Guid _varianteId;

    public VentasEnEsperaIntegrationTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        var cat = new Categoria { Nombre = "Ropa" };
        var prod = new Producto { Nombre = "Playera", CategoriaId = cat.Id };
        var v = new VarianteProducto { Producto = prod, Sku = new Sku("PLY-M"), Talla = "M", PrecioVenta = new Dinero(199m) };
        v.EstablecerStock(5);
        ctx.AddRange(cat, prod, v);
        ctx.SaveChanges();
        _varianteId = v.Id;
    }

    private VentasEnEsperaService Servicio(AppDbContext ctx)
    {
        var usuario = Substitute.For<ICurrentUser>();
        usuario.UsuarioId.Returns(_usuarioId);
        var reloj = Substitute.For<IDateTime>();
        reloj.UtcAhora.Returns(DateTime.UtcNow);
        return new VentasEnEsperaService(new VentaSuspendidaRepository(ctx), new VarianteRepository(ctx),
            new RepositoryBase<Cliente>(ctx), new RepositoryBase<Usuario>(ctx), new SesionCajaRepository(ctx),
            usuario, reloj, new UnitOfWork(ctx));
    }

    private NuevaVentaEsperaDto Carrito(int cantidad) => new("Cliente de rojo", null, null, 0, "talla M",
        new[] { new LineaEsperaDto(_varianteId, "Playera — M", "PLY-M", 199m, 199m, cantidad) });

    [Fact]
    public async Task Suspender_y_recuperar_tras_reiniciar_con_aviso_de_precio_y_existencia()
    {
        using (var ctx = new AppDbContext(_options))
            (await Servicio(ctx).SuspenderAsync(Carrito(8))).Exito.Should().BeTrue();

        // Mientras estaba en espera subió el precio.
        using (var ctx = new AppDbContext(_options))
        {
            var v = await ctx.Variantes.FirstAsync();
            v.PrecioVenta = new Dinero(249m);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = new AppDbContext(_options)) // "reinicio": contexto nuevo
        {
            var s = Servicio(ctx);
            (await s.ContarAsync()).Should().Be(1);
            var lista = await s.ListarAsync();
            var r = await s.RecuperarAsync(lista[0].Id);

            r.Exito.Should().BeTrue();
            r.Valor!.Venta.Lineas[0].PrecioVenta.Should().Be(249m);
            r.Valor.Avisos.Should().Contain(a => a.Contains("cambió de precio")).And.Contain(a => a.Contains("solo hay 5"));
            (await s.ContarAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task Borrador_es_uno_por_usuario_y_se_borra_con_carrito_vacio()
    {
        using var ctx = new AppDbContext(_options);
        var s = Servicio(ctx);
        await s.GuardarBorradorAsync(Carrito(1));
        await s.GuardarBorradorAsync(Carrito(2));
        (await s.ObtenerBorradorAsync())!.Venta.Articulos.Should().Be(2);
        (await s.ContarAsync()).Should().Be(0, "el borrador no cuenta como venta en espera");

        await s.EliminarBorradorAsync();
        (await s.ObtenerBorradorAsync()).Should().BeNull();
    }

    public void Dispose() => _conn.Dispose();
}
