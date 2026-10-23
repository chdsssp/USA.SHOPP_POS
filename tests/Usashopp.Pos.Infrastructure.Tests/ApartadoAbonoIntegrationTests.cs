using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>
/// Reproduce con EF real el bug de "agregar un abono a un apartado ya cargado" (los tests con
/// mocks no lo detectan).
/// </summary>
public class ApartadoAbonoIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;

    public ApartadoAbonoIntegrationTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private async Task<Guid> CrearApartadoConAnticipoAsync()
    {
        using var ctx = new AppDbContext(_options);
        var cliente = new Cliente { Nombre = "Cli" };
        ctx.Clientes.Add(cliente);
        var ap = new Apartado { ClienteId = cliente.Id, Folio = "A-1" };
        ap.Abonos.Add(new AbonoApartado { Monto = new Dinero(200m), Metodo = MetodoPago.Efectivo });
        ctx.Apartados.Add(ap);
        await ctx.SaveChangesAsync();
        return ap.Id;
    }

    [Fact]
    public async Task Abono_posterior_se_inserta_por_dbset()
    {
        // Así es como ApartadoService.AbonarAsync inserta el abono: por el DbSet (no por la
        // colección del apartado ya rastreado, que provocaba DbUpdateConcurrencyException).
        var apartadoId = await CrearApartadoConAnticipoAsync();

        using (var ctx = new AppDbContext(_options))
        {
            await ctx.Set<AbonoApartado>().AddAsync(
                new AbonoApartado { ApartadoId = apartadoId, Monto = new Dinero(800m), Metodo = MetodoPago.Transferencia });
            await ctx.SaveChangesAsync();
        }

        using var verif = new AppDbContext(_options);
        (await verif.Set<AbonoApartado>().CountAsync(a => a.ApartadoId == apartadoId)).Should().Be(2);
    }

    public void Dispose() => _conn.Dispose();
}
