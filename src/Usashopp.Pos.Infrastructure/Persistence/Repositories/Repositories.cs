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

    public async Task<IReadOnlyList<Venta>> ListarPorFechaAsync(DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        IQueryable<Venta> query = Set.Include(v => v.Detalles).Include(v => v.Pagos);
        if (desde is { } d) query = query.Where(v => v.Fecha >= d);
        if (hasta is { } h) query = query.Where(v => v.Fecha <= h);
        return await query.OrderByDescending(v => v.Fecha).Take(1000).ToListAsync(ct);
    }

    public Task<Venta?> ObtenerConDetalleAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(v => v.Detalles).Include(v => v.Pagos).FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Venta>> ListarPorSesionAsync(Guid sesionCajaId, CancellationToken ct = default) =>
        await Set.Include(v => v.Pagos)
                 .Where(v => v.SesionCajaId == sesionCajaId)
                 .ToListAsync(ct);
}

public class SesionCajaRepository : RepositoryBase<SesionCaja>, ISesionCajaRepository
{
    public SesionCajaRepository(AppDbContext db) : base(db) { }

    public Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.Estado == EstadoSesionCaja.Abierta, ct);

    public async Task<IReadOnlyList<SesionCaja>> ListarCerradasAsync(CancellationToken ct = default) =>
        await Set.Include(s => s.Ventas).ThenInclude(v => v.Pagos)
                 .Where(s => s.Estado == EstadoSesionCaja.Cerrada)
                 .OrderByDescending(s => s.FechaCierre)
                 .Take(300)
                 .ToListAsync(ct);
}

public class MovimientoInventarioRepository : RepositoryBase<MovimientoInventario>, IMovimientoInventarioRepository
{
    public MovimientoInventarioRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<MovimientoInventario>> ListarPorVarianteAsync(Guid varianteId, CancellationToken ct = default) =>
        await Set.Where(m => m.VarianteId == varianteId)
                 .OrderBy(m => m.Fecha)
                 .ToListAsync(ct);
}

public class CompraRepository : RepositoryBase<Compra>, ICompraRepository
{
    public CompraRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Compra>> ListarAsync(CancellationToken ct = default) =>
        await Set.Include(c => c.Proveedor).Include(c => c.Detalles)
                 .OrderByDescending(c => c.Fecha).Take(500).ToListAsync(ct);

    public Task<Compra?> ObtenerConDetalleAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(c => c.Proveedor)
           .Include(c => c.Detalles)!.ThenInclude(d => d.Variante)!.ThenInclude(v => v!.Producto)
           .FirstOrDefaultAsync(c => c.Id == id, ct);
}

public class ApartadoRepository : RepositoryBase<Apartado>, IApartadoRepository
{
    public ApartadoRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Apartado>> ListarAsync(CancellationToken ct = default) =>
        await Set.Include(a => a.Cliente).Include(a => a.Detalles).Include(a => a.Abonos)
                 .OrderByDescending(a => a.Fecha).Take(500).ToListAsync(ct);

    public Task<Apartado?> ObtenerConDetalleAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(a => a.Cliente).Include(a => a.Detalles).Include(a => a.Abonos)
           .FirstOrDefaultAsync(a => a.Id == id, ct);
}

public class UsuarioRepository : RepositoryBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AppDbContext db) : base(db) { }

    public Task<Usuario?> ObtenerPorLoginAsync(string login, CancellationToken ct = default) =>
        Set.Include(u => u.Rol)!.ThenInclude(r => r!.Permisos)
           .FirstOrDefaultAsync(u => u.UsuarioLogin == login, ct);

    public async Task<IReadOnlyList<Usuario>> ListarConRolAsync(CancellationToken ct = default) =>
        await Set.Include(u => u.Rol).Where(u => u.Activo).OrderBy(u => u.Nombre).ToListAsync(ct);

    public Task<bool> ExisteLoginAsync(string login, Guid? exceptoId = null, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.UsuarioLogin == login && (exceptoId == null || u.Id != exceptoId), ct);
}

public class RolRepository : RepositoryBase<Rol>, IRolRepository
{
    public RolRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Rol>> ListarConPermisosAsync(CancellationToken ct = default) =>
        await Set.Include(r => r.Permisos).OrderBy(r => r.Nombre).ToListAsync(ct);
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
