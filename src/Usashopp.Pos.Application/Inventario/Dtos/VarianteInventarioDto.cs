using System.Linq;

namespace Usashopp.Pos.Application.Inventario.Dtos;

/// <summary>Fila de la pantalla de Inventario (una variante con su producto).</summary>
public record VarianteInventarioDto(
    Guid VarianteId,
    Guid ProductoId,
    string Producto,
    string? Categoria,
    string? Marca,
    string? Talla,
    string? Color,
    string Sku,
    string? CodigoBarras,
    decimal Precio,
    decimal Costo,
    int Stock,
    int StockMinimo,
    bool BajoStock,
    bool Activo)
{
    /// <summary>Descripción legible con el orden "Marca Talla Nombre Color" (omite las partes vacías).</summary>
    public string DescripcionCompleta =>
        string.Join(" ", new[] { Marca, Talla, Producto, Color }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
