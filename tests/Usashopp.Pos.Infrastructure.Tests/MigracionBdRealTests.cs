using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Infrastructure.Persistence;
using Usashopp.Pos.Infrastructure.Persistence.Seed;
using Usashopp.Pos.Infrastructure.System;
using Xunit;
using Xunit.Abstractions;

namespace Usashopp.Pos.Infrastructure.Tests;

/// <summary>
/// Regla de los lotes [BD]: probar cada migración sobre una COPIA del pos.db real antes de darla
/// por buena. Si el equipo no tiene la base real (p. ej. en CI), la prueba no hace nada.
/// La base original nunca se abre: se copia a una carpeta temporal.
/// </summary>
public class MigracionBdRealTests
{
    private static readonly string RutaReal = Environment.GetEnvironmentVariable("USASHOPP_BD_REAL")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "USASHOPP POS", "data", "pos.db");

    private readonly ITestOutputHelper _out;
    public MigracionBdRealTests(ITestOutputHelper output) => _out = output;

    private sealed class SinExterna : IPreferenciasRespaldo { public string? CarpetaRespaldoExterno => null; }

    [Fact]
    public async Task Migraciones_pendientes_se_aplican_sin_perder_datos_sobre_copia_de_la_bd_real()
    {
        if (!File.Exists(RutaReal)) { _out.WriteLine("Sin base real en este equipo: se omite."); return; }

        var carpeta = Path.Combine(Path.GetTempPath(), "usashopp-mig-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        var copia = Path.Combine(carpeta, "pos.db");
        // Copia consistente aunque la app esté abierta (no se escribe en la original).
        using (var o = new SqliteConnection($"Data Source={RutaReal};Mode=ReadOnly;Pooling=False"))
        using (var d = new SqliteConnection($"Data Source={copia};Pooling=False"))
        {
            o.Open(); d.Open(); o.BackupDatabase(d);
        }

        var antes = Contar(copia);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={copia}").Options;
        using (var ctx = new AppDbContext(options))
        {
            var pendientes = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
            _out.WriteLine("Pendientes: " + string.Join(", ", pendientes));
            var backup = new SqliteBackupService(ctx,
                Options.Create(new InfrastructureOptions { CarpetaRespaldos = Path.Combine(carpeta, "backups") }), new SinExterna());
            await new DatabaseInitializer(ctx, new PasswordHasher(), backup).InicializarAsync();
            (await ctx.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }

        var despues = Contar(copia);
        foreach (var (tabla, n) in antes)
        {
            _out.WriteLine($"{tabla}: {n} → {despues[tabla]}");
            despues[tabla].Should().BeGreaterThanOrEqualTo(n, $"la migración no debe perder filas de {tabla}");
        }

        SqliteConnection.ClearAllPools();
        try { Directory.Delete(carpeta, true); } catch { /* temporal */ }
    }

    private static Dictionary<string, long> Contar(string ruta)
    {
        var tablas = new[] { "Ventas", "DetallesVenta", "Pagos", "Variantes", "Productos", "Clientes", "Usuarios", "MovimientosInventario" };
        var r = new Dictionary<string, long>();
        using var c = new SqliteConnection($"Data Source={ruta};Mode=ReadOnly;Pooling=False");
        c.Open();
        foreach (var t in tablas)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{t}\"";
            try { r[t] = Convert.ToInt64(cmd.ExecuteScalar()); } catch { r[t] = 0; }
        }
        return r;
    }
}
