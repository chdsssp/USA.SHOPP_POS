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

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();

    public DbSet<ConfiguracionTienda> Configuracion => Set<ConfiguracionTienda>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Convertidores globales de value objects.
        builder.Properties<Dinero>().HaveConversion<DineroConverter>().HavePrecision(18, 2);
        builder.Properties<Sku>().HaveConversion<SkuConverter>().HaveMaxLength(64);
        builder.Properties<CodigoBarras>().HaveConversion<CodigoBarrasConverter>().HaveMaxLength(64);
        builder.Properties<Descuento>().HaveConversion<DescuentoConverter>().HaveMaxLength(32);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Aplica todas las clases IEntityTypeConfiguration<> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
