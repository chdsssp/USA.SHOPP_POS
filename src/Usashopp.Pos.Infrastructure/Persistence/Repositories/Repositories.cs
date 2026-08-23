using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Infrastructure.Persistence.Repositories;

public class ProductoRepository : RepositoryBase<Producto>, IProductoRepository
{
    public ProductoRepository(AppDbContext db) : base(db) { }

    public Task<Producto?> ObtenerConVariantesAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(p => p.Variantes).FirstOrDefaultAsync(p => p.Id == id, ct);
}

public class VentaRepository : RepositoryBase<Venta>, IVentaRepository
{
    public VentaRepository(AppDbContext db) : base(db) { }

    public Task<Venta?> ObtenerPorFolioAsync(string folio, CancellationToken ct = default) =>
        Set.Include(v => v.Detalles).Include(v => v.Pagos)
           .FirstOrDefaultAsync(v => v.Folio == folio, ct);
}

public class SesionCajaRepository : RepositoryBase<SesionCaja>, ISesionCajaRepository
{
    public SesionCajaRepository(AppDbContext db) : base(db) { }

    public Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.Estado == EstadoSesionCaja.Abierta, ct);
}

public class MovimientoInventarioRepository : RepositoryBase<MovimientoInventario>, IMovimientoInventarioRepository
{
    public MovimientoInventarioRepository(AppDbContext db) : base(db) { }
}

public class ConfiguracionTiendaRepository : IConfiguracionTiendaRepository
{
    private readonly AppDbContext _db;
    public ConfiguracionTiendaRepository(AppDbContext db) => _db = db;

    public async Task<ConfiguracionTienda> ObtenerAsync(CancellationToken ct = default)
    {
        var config = await _db.Configuracion.FirstOrDefaultAsync(ct);
        if (config is null)
        {
            config = new ConfiguracionTienda();
            await _db.Configuracion.AddAsync(config, ct);
            await _db.SaveChangesAsync(ct);
        }
        return config;
    }
}
