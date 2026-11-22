using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Infrastructure.Persistence.Repositories;

/// <summary>
/// Índice en memoria para la búsqueda del POS. Antes cada búsqueda (cada tecla, con un pequeño
/// debounce) cargaba TODAS las variantes con su producto desde SQLite. Ahora se guarda una sola
/// vez el texto normalizado de cada variante y solo se recarga cuando cambia el catálogo
/// (<see cref="Invalidar"/>, llamado desde <see cref="AppDbContext"/>); vender (cambio de stock)
/// no lo invalida.
/// </summary>
internal static class IndiceBusquedaVariantes
{
    private sealed record Entrada(Guid Id, string Nombre, string Talla, string[] Campos);

    private static readonly object Candado = new();
    private static int _version;
    private static readonly Dictionary<string, (int Version, List<Entrada> Entradas)> PorBase = new();

    /// <summary>El catálogo cambió (producto, variante, categoría): el índice se recarga en la próxima búsqueda.</summary>
    public static void Invalidar() => Interlocked.Increment(ref _version);

    public static async Task<IReadOnlyList<Guid>> BuscarAsync(AppDbContext db, string[] tokens, int limite, CancellationToken ct)
    {
        var entradas = await ObtenerAsync(db, ct);
        return entradas
            .Where(e => BusquedaTexto.Coincide(tokens, e.Campos))
            .OrderBy(e => e.Nombre).ThenBy(e => e.Talla)
            .Take(limite)
            .Select(e => e.Id)
            .ToList();
    }

    private static async Task<List<Entrada>> ObtenerAsync(AppDbContext db, CancellationToken ct)
    {
        var clave = db.Database.GetDbConnection().DataSource;
        var enMemoria = string.IsNullOrEmpty(clave) || clave.Contains(":memory:", StringComparison.OrdinalIgnoreCase);
        var version = Volatile.Read(ref _version);

        if (!enMemoria)
            lock (Candado)
                if (PorBase.TryGetValue(clave, out var c) && c.Version == version)
                    return c.Entradas;

        var filas = await db.Set<VarianteProducto>()
            .AsNoTracking()
            .Where(v => v.Activo)
            .Select(v => new { v.Id, Nombre = v.Producto!.Nombre, Marca = v.Producto.Marca, v.Talla, v.Color, v.Sku, v.CodigoBarras })
            .ToListAsync(ct);

        var lista = filas.Select(f => new Entrada(
            f.Id, f.Nombre ?? string.Empty, f.Talla ?? string.Empty,
            new[] { f.Nombre, f.Marca, f.Talla, f.Color, f.Sku.Valor, f.CodigoBarras?.Valor })).ToList();

        if (!enMemoria)
            lock (Candado)
                PorBase[clave] = (version, lista);
        return lista;
    }
}
