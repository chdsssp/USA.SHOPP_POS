using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>
/// Antes, <c>ListarPorFechaAsync</c> cortaba en 1000 ventas y los reportes de periodos largos
/// salían mal sin aviso. Estas pruebas usan SQLite real.
/// </summary>
public class VentaRepositoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private static readonly DateTime Base = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public VentaRepositoryIntegrationTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private async Task SembrarVentasAsync(int n)
    {
        using var ctx = new AppDbContext(_options);
        var categoria = new Categoria { Nombre = "Ropa" };
        ctx.Add(categoria);
        var producto = new Producto { Nombre = "Playera", CategoriaId = categoria.Id };
        var variante = new VarianteProducto { Producto = producto, Sku = new Sku("PLY-1"), PrecioVenta = new Dinero(10m) };
        ctx.Add(producto);
        ctx.Add(variante);
        var sesion = new SesionCaja();
        ctx.Add(sesion);
        for (var i = 0; i < n; i++)
        {
            var v = new Venta { Folio = $"V-{i:D6}", SesionCajaId = sesion.Id, Fecha = Base.AddMinutes(i) };
            v.Detalles.Add(new DetalleVenta { VarianteId = variante.Id, Descripcion = "Playera", Cantidad = 1, PrecioUnitario = new Dinero(10m) });
            v.Pagos.Add(new Pago { Metodo = MetodoPago.Efectivo, Monto = new Dinero(10m) });
            v.MarcarPagada();
            ctx.Add(v);
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Listar_por_fecha_no_tiene_tope_de_1000()
    {
        await SembrarVentasAsync(1250);
        using var ctx = new AppDbContext(_options);
        var repo = new VentaRepository(ctx);

        var ventas = await repo.ListarPorFechaAsync(Base.AddDays(-1), Base.AddDays(2));

        ventas.Should().HaveCount(1250);
        ventas.Sum(v => v.Total.Monto).Should().Be(12_500m);
    }

    [Fact]
    public async Task Pagina_y_conteo_del_historial()
    {
        await SembrarVentasAsync(450);
        using var ctx = new AppDbContext(_options);
        var repo = new VentaRepository(ctx);

        (await repo.ContarPorFechaAsync(null, null)).Should().Be(450);
        var p1 = await repo.ListarPaginaPorFechaAsync(null, null, 0, 200);
        var p3 = await repo.ListarPaginaPorFechaAsync(null, null, 400, 200);

        p1.Should().HaveCount(200);
        p1[0].Folio.Should().Be("V-000449"); // más reciente primero
        p3.Should().HaveCount(50);
    }

    public void Dispose() => _conn.Dispose();
}
