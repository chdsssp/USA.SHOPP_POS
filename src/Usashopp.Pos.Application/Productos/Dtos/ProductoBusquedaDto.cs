namespace Usashopp.Pos.Application.Productos.Dtos;

/// <summary>Resultado de búsqueda mostrado en el POS (una variante).</summary>
public record ProductoBusquedaDto(
    Guid VarianteId,
    string Descripcion,
    string Sku,
    string? CodigoBarras,
    decimal Precio,
    int Stock,
    bool BajoStock,
    string? ImagenRuta = null,
    Guid? ProductoId = null,
    Guid? CategoriaId = null);

/// <summary>Tarjeta del grid del POS: un producto (no cada variante).</summary>
public record ProductoGridDto(
    Guid ProductoId,
    string Nombre,
    string? ImagenRuta,
    decimal PrecioMin,
    decimal PrecioMax,
    int StockTotal,
    int NumVariantes,
    bool BajoStock,
    ProductoBusquedaDto? VarianteUnica)
{
    public string PrecioTexto => PrecioMin == PrecioMax ? PrecioMin.ToString("C2") : $"{PrecioMin:C0} – {PrecioMax:C0}";
    public bool TieneVariantes => NumVariantes > 1;
}

public record PaginaProductosGridDto(IReadOnlyList<ProductoGridDto> Items, int Total)
{
    public bool HayMas => Items.Count < Total;
}

/// <summary>Una casilla de la tabla talla × color.</summary>
public record CeldaVarianteDto(string Talla, string Color, ProductoBusquedaDto? Variante)
{
    public bool Existe => Variante is not null;
    public bool Agotada => Variante is { Stock: <= 0 };
    public bool BajoStock => Variante is { BajoStock: true, Stock: > 0 };
}

/// <summary>Tabla talla × color de un producto (filas = colores, columnas = tallas).</summary>
public record MatrizVariantesDto(
    Guid ProductoId,
    string Nombre,
    string? ImagenRuta,
    IReadOnlyList<string> Tallas,
    IReadOnlyList<string> Colores,
    IReadOnlyList<IReadOnlyList<CeldaVarianteDto>> Filas);
