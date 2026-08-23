using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IProductoRepository : IRepository<Producto>
{
    /// <summary>Obtiene un producto con sus variantes cargadas.</summary>
    Task<Producto?> ObtenerConVariantesAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Consultas de variantes optimizadas para el POS (búsqueda instantánea).</summary>
public interface IVarianteRepository : IRepository<VarianteProducto>
{
    /// <summary>Búsqueda exacta por código de barras (uso con lector).</summary>
    Task<VarianteProducto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);

    /// <summary>Búsqueda exacta por SKU.</summary>
    Task<VarianteProducto?> ObtenerPorSkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>Búsqueda por texto libre (nombre, marca, talla, color, SKU).</summary>
    Task<IReadOnlyList<VarianteProducto>> BuscarAsync(string texto, int limite = 50, CancellationToken cancellationToken = default);

    /// <summary>Listado para la pantalla de Inventario, con producto y categoría cargados.</summary>
    Task<IReadOnlyList<VarianteProducto>> ListarInventarioAsync(
        string? texto = null,
        bool soloBajoStock = false,
        bool incluirInactivas = false,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si ya existe una variante con ese SKU (para validar duplicados).</summary>
    Task<bool> ExisteSkuAsync(string sku, Guid? exceptoId = null, CancellationToken cancellationToken = default);

    /// <summary>Listado para el grid táctil del POS, opcionalmente filtrado por categoría.</summary>
    Task<IReadOnlyList<VarianteProducto>> ListarParaVentaAsync(
        Guid? categoriaId = null, int limite = 200, CancellationToken cancellationToken = default);
}
