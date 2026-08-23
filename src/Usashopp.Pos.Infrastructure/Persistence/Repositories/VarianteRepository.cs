using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Infrastructure.Persistence.Repositories;

public class VarianteRepository : RepositoryBase<VarianteProducto>, IVarianteRepository
{
    public VarianteRepository(AppDbContext db) : base(db) { }

    public Task<VarianteProducto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
    {
        // Se compara el value object completo: EF aplica el convertidor a ambos lados.
        CodigoBarras cb = new(codigoBarras);
        return Set.Include(v => v.Producto)
                  .FirstOrDefaultAsync(v => v.Activo && v.CodigoBarras == cb, ct);
    }

    public Task<VarianteProducto?> ObtenerPorSkuAsync(string sku, CancellationToken ct = default)
    {
        Sku s = new(sku);
        return Set.Include(v => v.Producto)
                  .FirstOrDefaultAsync(v => v.Activo && v.Sku == s, ct);
    }

    public async Task<IReadOnlyList<VarianteProducto>> BuscarAsync(string texto, int limite = 50, CancellationToken ct = default)
    {
        // La búsqueda por texto libre cubre nombre, marca y atributos. El SKU y el código
        // de barras se resuelven por coincidencia exacta (lector) en los métodos de arriba.
        var t = texto.Trim();
        return await Set
            .Include(v => v.Producto)
            .Where(v => v.Activo &&
                (EF.Functions.Like(v.Producto!.Nombre, $"%{t}%") ||
                 (v.Producto!.Marca != null && EF.Functions.Like(v.Producto.Marca, $"%{t}%")) ||
                 (v.Talla != null && EF.Functions.Like(v.Talla, $"%{t}%")) ||
                 (v.Color != null && EF.Functions.Like(v.Color, $"%{t}%"))))
            .OrderBy(v => v.Producto!.Nombre)
            .Take(limite)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VarianteProducto>> ListarInventarioAsync(
        string? texto = null, bool soloBajoStock = false, bool incluirInactivas = false, CancellationToken ct = default)
    {
        IQueryable<VarianteProducto> query = Set
            .Include(v => v.Producto)!.ThenInclude(p => p!.Categoria);

        if (!incluirInactivas)
            query = query.Where(v => v.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Producto!.Nombre, $"%{t}%") ||
                (v.Producto!.Marca != null && EF.Functions.Like(v.Producto.Marca, $"%{t}%")) ||
                (v.Talla != null && EF.Functions.Like(v.Talla, $"%{t}%")) ||
                (v.Color != null && EF.Functions.Like(v.Color, $"%{t}%")));
        }

        if (soloBajoStock)
            query = query.Where(v => v.StockActual <= v.StockMinimo);

        return await query
            .OrderBy(v => v.Producto!.Nombre).ThenBy(v => v.Talla)
            .Take(500)
            .ToListAsync(ct);
    }

    public Task<bool> ExisteSkuAsync(string sku, Guid? exceptoId = null, CancellationToken ct = default)
    {
        Sku s = new(sku);
        return Set.AnyAsync(v => v.Sku == s && (exceptoId == null || v.Id != exceptoId), ct);
    }

    public async Task<IReadOnlyList<VarianteProducto>> ListarParaVentaAsync(
        Guid? categoriaId = null, int limite = 200, CancellationToken ct = default)
    {
        IQueryable<VarianteProducto> query = Set
            .Include(v => v.Producto)
            .Where(v => v.Activo);

        if (categoriaId is { } cat)
            query = query.Where(v => v.Producto!.CategoriaId == cat);

        return await query
            .OrderBy(v => v.Producto!.Nombre).ThenBy(v => v.Talla)
            .Take(limite)
            .ToListAsync(ct);
    }
}
