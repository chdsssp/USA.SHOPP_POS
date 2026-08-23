namespace Usashopp.Pos.Application.Productos.Dtos;

/// <summary>Resultado de búsqueda mostrado en el POS (una variante).</summary>
public record ProductoBusquedaDto(
    Guid VarianteId,
    string Descripcion,
    string Sku,
    string? CodigoBarras,
    decimal Precio,
    int Stock,
    bool BajoStock);
