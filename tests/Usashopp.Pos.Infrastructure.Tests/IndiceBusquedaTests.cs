using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>El índice en memoria de la búsqueda se recarga cuando cambia el catálogo (Lote 20).</summary>
public class IndiceBusquedaTests : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"usashopp-idx-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<AppDbContext> _options;

    public IndiceBusquedaTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_ruta}").Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        var cat = new Categoria { Nombre = "Ropa" };
        var p = new Producto { Nombre = "Playera básica", CategoriaId = cat.Id };
        var v = new VarianteProducto { Producto = p, Sku = new Sku("PLY-NEG-M"), Talla = "M", Color = "Negro", PrecioVenta = new Dinero(199m) };
        v.EstablecerStock(3);
        ctx.AddRange(cat, p, v);
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Busca_sin_acentos_y_se_actualiza_al_cambiar_el_catalogo()
    {
        using (var ctx = new AppDbContext(_options))
        {
            var repo = new VarianteRepository(ctx);
            (await repo.BuscarAsync("basica negro")).Should().HaveCount(1);
            (await repo.BuscarAsync("ply-neg")).Should().HaveCount(1);
        }

        // Vender (cambiar stock) no altera la búsqueda; renombrar el producto sí.
        using (var ctx = new AppDbContext(_options))
        {
            var v = await ctx.Variantes.Include(x => x.Producto).FirstAsync();
            v.AplicarCambioStock(-1);
            v.Producto!.Nombre = "Camiseta básica";
            await ctx.SaveChangesAsync();
        }

        using (var ctx = new AppDbContext(_options))
        {
            var repo = new VarianteRepository(ctx);
            (await repo.BuscarAsync("camiseta")).Should().HaveCount(1);
            (await repo.BuscarAsync("playera")).Should().BeEmpty();
            (await repo.BuscarAsync("camiseta")).Single().StockActual.Should().Be(2);
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_ruta); } catch { /* temporal */ }
    }
}
