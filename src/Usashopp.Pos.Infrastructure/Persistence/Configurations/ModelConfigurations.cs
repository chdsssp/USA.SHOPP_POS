using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Infrastructure.Persistence.Configurations;

public class CategoriaConfig : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
        b.HasIndex(c => c.Nombre).IsUnique();
    }
}

public class ProductoConfig : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        b.Property(p => p.Marca).HasMaxLength(100);
        b.HasIndex(p => p.Nombre);
        b.HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VarianteProductoConfig : IEntityTypeConfiguration<VarianteProducto>
{
    public void Configure(EntityTypeBuilder<VarianteProducto> b)
    {
        b.Property(v => v.Talla).HasMaxLength(40);
        b.Property(v => v.Color).HasMaxLength(40);

        // Búsquedas instantáneas del POS.
        b.HasIndex(v => v.Sku).IsUnique();
        b.HasIndex(v => v.CodigoBarras).IsUnique();
        b.HasIndex(v => v.ProductoId);

        b.HasOne(v => v.Producto)
            .WithMany(p => p.Variantes)
            .HasForeignKey(v => v.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MovimientoInventarioConfig : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> b)
    {
        b.HasIndex(m => m.VarianteId);
        b.HasIndex(m => m.Fecha);
        b.HasOne(m => m.Variante)
            .WithMany()
            .HasForeignKey(m => m.VarianteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
        b.HasIndex(c => c.Nombre);
    }
}

public class VentaConfig : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.Property(v => v.Folio).IsRequired().HasMaxLength(30);
        b.HasIndex(v => v.Folio).IsUnique();
        b.HasIndex(v => v.Fecha);

        b.HasMany(v => v.Detalles).WithOne().HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Pagos).WithOne().HasForeignKey(p => p.VentaId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DetalleVentaConfig : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> b)
    {
        b.Property(d => d.Descripcion).IsRequired().HasMaxLength(250);
        b.Ignore(d => d.Importe); // calculado
    }
}

public class ApartadoConfig : IEntityTypeConfiguration<Apartado>
{
    public void Configure(EntityTypeBuilder<Apartado> b)
    {
        b.Property(a => a.Folio).IsRequired().HasMaxLength(30);
        b.HasIndex(a => a.Folio).IsUnique();
        b.HasMany(a => a.Detalles).WithOne().HasForeignKey(d => d.ApartadoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(a => a.Abonos).WithOne().HasForeignKey(a => a.ApartadoId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(a => a.Total);
        b.Ignore(a => a.TotalAbonado);
        b.Ignore(a => a.Saldo);
    }
}

public class DetalleApartadoConfig : IEntityTypeConfiguration<DetalleApartado>
{
    public void Configure(EntityTypeBuilder<DetalleApartado> b)
    {
        b.Property(d => d.Descripcion).HasMaxLength(250);
        b.Ignore(d => d.Importe);
    }
}

public class CompraConfig : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> b)
    {
        b.Property(c => c.Folio).IsRequired().HasMaxLength(30);
        b.HasIndex(c => c.Folio).IsUnique();
        b.HasMany(c => c.Detalles).WithOne().HasForeignKey(d => d.CompraId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Proveedor).WithMany().HasForeignKey(c => c.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(c => c.Total);
    }
}

public class DetalleCompraConfig : IEntityTypeConfiguration<DetalleCompra>
{
    public void Configure(EntityTypeBuilder<DetalleCompra> b) => b.Ignore(d => d.Importe);
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(u => u.Nombre).IsRequired().HasMaxLength(150);
        b.Property(u => u.UsuarioLogin).IsRequired().HasMaxLength(60);
        b.HasIndex(u => u.UsuarioLogin).IsUnique();
        b.HasOne(u => u.Rol).WithMany(r => r.Usuarios).HasForeignKey(u => u.RolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> b)
    {
        b.Property(r => r.Nombre).IsRequired().HasMaxLength(60);
        b.HasIndex(r => r.Nombre).IsUnique();
        b.HasMany(r => r.Permisos).WithMany(p => p.Roles);
    }
}

public class PermisoConfig : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> b)
    {
        b.Property(p => p.Clave).IsRequired().HasMaxLength(60);
        b.HasIndex(p => p.Clave).IsUnique();
    }
}
