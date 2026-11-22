using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Usashopp.Pos.Infrastructure.Persistence.Converters;

namespace Usashopp.Pos.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<VarianteProducto> Variantes => Set<VarianteProducto>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<NotaCredito> NotasCredito => Set<NotaCredito>();
    public DbSet<ConsumoNotaCredito> ConsumosNotaCredito => Set<ConsumoNotaCredito>();
    public DbSet<AbonoCliente> AbonosCliente => Set<AbonoCliente>();

    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<Pago> Pagos => Set<Pago>();

    public DbSet<Apartado> Apartados => Set<Apartado>();
    public DbSet<DetalleApartado> DetallesApartado => Set<DetalleApartado>();
    public DbSet<AbonoApartado> AbonosApartado => Set<AbonoApartado>();

    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
    public DbSet<PagoCompra> PagosCompra => Set<PagoCompra>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();

    public DbSet<ConfiguracionTienda> Configuracion => Set<ConfiguracionTienda>();

    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public DbSet<HistorialPrecio> HistorialPrecios => Set<HistorialPrecio>();

    public DbSet<VentaSuspendida> VentasSuspendidas => Set<VentaSuspendida>();

    public DbSet<Promocion> Promociones => Set<Promocion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Convertidores globales de value objects.
        builder.Properties<Dinero>().HaveConversion<DineroConverter>().HavePrecision(18, 2);
        builder.Properties<Sku>().HaveConversion<SkuConverter>().HaveMaxLength(64);
        builder.Properties<CodigoBarras>().HaveConversion<CodigoBarrasConverter>().HaveMaxLength(64);
        builder.Properties<Descuento>().HaveConversion<DescuentoConverter>().HaveMaxLength(32);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var catalogo = CambiaCatalogo();
        var r = base.SaveChanges(acceptAllChangesOnSuccess);
        if (catalogo) Repositories.IndiceBusquedaVariantes.Invalidar();
        return r;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var catalogo = CambiaCatalogo();
        var r = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (catalogo) Repositories.IndiceBusquedaVariantes.Invalidar();
        return r;
    }

    /// <summary>
    /// ¿Cambia algo que afecta la búsqueda del POS? Productos/categorías, o variantes nuevas/borradas
    /// o editadas en algo distinto del stock (vender solo mueve stock y no debe recargar el índice).
    /// </summary>
    private bool CambiaCatalogo()
    {
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State is EntityState.Unchanged or EntityState.Detached) continue;
            if (e.Entity is Producto or Categoria) return true;
            if (e.Entity is VarianteProducto)
            {
                if (e.State is EntityState.Added or EntityState.Deleted) return true;
                if (e.Properties.Any(p => p.IsModified &&
                        p.Metadata.Name is not (nameof(VarianteProducto.StockActual) or nameof(VarianteProducto.ActualizadoEn))))
                    return true;
            }
        }
        return false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Aplica todas las clases IEntityTypeConfiguration<> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
