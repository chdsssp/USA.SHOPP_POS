using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Infrastructure.System;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>Respaldos con SQLite real en archivo (Lote 23).</summary>
public class RespaldosIntegrationTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "usashopp-resp-" + Guid.NewGuid().ToString("N"));
    private readonly string _rutaDb;
    private readonly DbContextOptions<AppDbContext> _options;

    public RespaldosIntegrationTests()
    {
        Directory.CreateDirectory(_carpeta);
        _rutaDb = Path.Combine(_carpeta, "pos.db");
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_rutaDb}").Options;
    }

    private sealed class Prefs : IPreferenciasRespaldo
    {
        public string? CarpetaRespaldoExterno { get; init; }
    }

    private SqliteBackupService Servicio(AppDbContext ctx, string? externa = null) => new(
        ctx,
        Options.Create(new InfrastructureOptions { CarpetaRespaldos = Path.Combine(_carpeta, "backups"), RetenerUltimos = 30 }),
        new Prefs { CarpetaRespaldoExterno = externa });

    [Fact]
    public async Task Respaldo_en_linea_se_verifica_y_copia_a_la_carpeta_externa()
    {
        using var ctx = new AppDbContext(_options);
        await ctx.Database.MigrateAsync();
        ctx.Categorias.Add(new Categoria { Nombre = "Ropa" });
        await ctx.SaveChangesAsync();

        var externa = Path.Combine(_carpeta, "usb");
        var servicio = Servicio(ctx, externa);
        var ruta = await servicio.CrearRespaldoAsync(OrigenRespaldo.Corte);

        servicio.VerificarRespaldo(ruta).Should().BeTrue();
        servicio.ListarRespaldos().Should().ContainSingle(r => r.Origen == OrigenRespaldo.Corte && r.Verificado == true);
        Directory.GetFiles(externa, "pos_*.db").Should().HaveCount(1);
        servicio.ObtenerEstadoExterno().Ultimo.Should().NotBeNull();

        // El respaldo contiene los datos.
        using var c = new SqliteConnection($"Data Source={ruta};Mode=ReadOnly;Pooling=False");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Categorias";
        Convert.ToInt32(cmd.ExecuteScalar()).Should().Be(1);
    }

    [Fact]
    public void Archivo_danado_no_pasa_la_verificacion()
    {
        var falso = Path.Combine(_carpeta, "pos_falso.db");
        File.WriteAllBytes(falso, Enumerable.Repeat((byte)0x42, 4096).ToArray());
        using var ctx = new AppDbContext(_options);
        Servicio(ctx).VerificarRespaldo(falso).Should().BeFalse();
    }

    [Fact]
    public async Task Antes_de_migrar_una_base_con_datos_se_crea_respaldo_previo()
    {
        // Base "vieja": migrada hasta la penúltima migración.
        using (var ctx = new AppDbContext(_options))
        {
            var migraciones = ctx.Database.GetMigrations().ToList();
            await ctx.GetService<IMigrator>().MigrateAsync(migraciones[^2]);
        }

        using (var ctx = new AppDbContext(_options))
        {
            var servicio = Servicio(ctx);
            var init = new DatabaseInitializer(ctx, new PasswordHasher(), servicio);
            await init.InicializarAsync();

            (await ctx.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
            servicio.ListarRespaldos().Should().Contain(r => r.Origen == OrigenRespaldo.PreMigracion && r.Verificado == true);
        }
    }

    [Fact]
    public void Retencion_escalonada_conserva_recientes_semanales_mensuales_y_premigracion()
    {
        var ahora = new DateTime(2026, 11, 21, 12, 0, 0);
        var lista = new List<(string, DateTime, OrigenRespaldo)>();
        for (var d = 0; d < 400; d++)
            lista.Add(($"r{d}", ahora.AddDays(-d), d == 300 ? OrigenRespaldo.PreMigracion : OrigenRespaldo.Automatico));

        var borrar = SqliteBackupService.SeleccionarParaBorrar(lista, ahora, minimoRecientes: 5).ToHashSet();

        borrar.Should().NotContain(Enumerable.Range(0, 15).Select(d => $"r{d}")); // últimos 14 días
        borrar.Should().NotContain("r300");                                       // pre-migración
        borrar.Should().Contain("r399");                                          // > 12 meses
        lista.Count(x => !borrar.Contains(x.Item1)).Should().BeLessThan(45);      // ≈ 15 + 8 + 12 + 1
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_carpeta, true); } catch { /* archivos aún abiertos: no importa */ }
    }
}
