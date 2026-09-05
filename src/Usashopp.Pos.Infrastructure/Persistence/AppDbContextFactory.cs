using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Usashopp.Pos.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada SOLO por las herramientas de EF Core (dotnet ef) en tiempo de diseño,
/// para poder generar migraciones sin arrancar la app WPF. En ejecución, el DbContext
/// se crea por inyección de dependencias con la cadena de conexión real.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=pos-design.db")
            .Options;
        return new AppDbContext(options);
    }
}
