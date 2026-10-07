using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Infrastructure.Persistence.Repositories;

public class VarianteRepository : RepositoryBase<VarianteProducto>, IVarianteRepository
{
    public VarianteRepository(AppDbContext db) : base(db) { }

    /// <summary>
    /// Incluye el Producto para que <see cref="VarianteProducto.DescripcionCompleta"/> tenga
    /// marca y nombre (si no, la descripción congelada de la venta/apartado saldría como "Producto").
    /// </summary>
    public override Task<VarianteProducto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == id, ct);

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
        var tokens = BusquedaTexto.Tokens(texto);
        if (tokens.Length == 0) return Array.Empty<VarianteProducto>();

        // Se filtra en memoria para poder buscar en TODAS las propiedades (incluidos SKU y código
        // de barras, que son value objects y no se pueden consultar con LIKE en SQL), de forma
        // insensible a acentos/mayúsculas y con varias palabras.
        var activas = await Set.Include(v => v.Producto).Where(v => v.Activo).ToListAsync(ct);
        return activas
            .Where(v => Coincide(v, tokens))
            .OrderBy(v => v.Producto?.Nombre)
            .Take(limite)
            .ToList();
    }

    public async Task<IReadOnlyList<VarianteProducto>> ListarInventarioAsync(
        string? texto = null, bool soloBajoStock = false, bool incluirInactivas = false, CancellationToken ct = default)
    {
        IQueryable<VarianteProducto> query = Set
            .Include(v => v.Producto)!.ThenInclude(p => p!.Categoria);

        if (!incluirInactivas)
            query = query.Where(v => v.Activo);

        if (soloBajoStock)
            query = query.Where(v => v.StockActual <= v.StockMinimo);

        var lista = await query.ToListAsync(ct);

        // El texto filtra en memoria sobre todas las propiedades (incluidos SKU y código de barras).
        var tokens = BusquedaTexto.Tokens(texto);
        if (tokens.Length > 0)
            lista = lista.Where(v => Coincide(v, tokens)).ToList();

        return lista
            .OrderBy(v => v.Producto?.Nombre).ThenBy(v => v.Talla)
            .Take(500)
            .ToList();
    }

    private static bool Coincide(VarianteProducto v, string[] tokens) =>
        BusquedaTexto.Coincide(tokens,
            v.Producto?.Nombre, v.Producto?.Marca, v.Talla, v.Color, v.Sku.Valor, v.CodigoBarras?.Valor);

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
