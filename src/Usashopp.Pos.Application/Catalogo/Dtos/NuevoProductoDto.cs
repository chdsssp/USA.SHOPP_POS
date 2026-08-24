namespace Usashopp.Pos.Application.Catalogo.Dtos;

/// <summary>Datos para dar de alta o editar una variante dentro de un producto.</summary>
public record VarianteEntradaDto(
    string Sku,
    string? CodigoBarras,
    string? Talla,
    string? Color,
    decimal Precio,
    decimal Costo,
    int StockInicial,
    int StockMinimo,
    Guid? Id = null);

/// <summary>Datos para dar de alta un producto con sus variantes.</summary>
public record NuevoProductoDto(
    string Nombre,
    Guid CategoriaId,
    IReadOnlyList<VarianteEntradaDto> Variantes,
    string? Descripcion = null,
    string? Marca = null);

/// <summary>Producto cargado para edición (variantes con su Id).</summary>
public record ProductoEdicionDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    string? Marca,
    Guid CategoriaId,
    IReadOnlyList<VarianteEntradaDto> Variantes);
