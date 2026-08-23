using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Producto del catálogo. Agrupa una o más <see cref="VarianteProducto"/> (talla/color);
/// el precio y el stock viven en la variante.
/// </summary>
public class Producto : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Marca { get; set; }
    public bool Activo { get; set; } = true;

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
}
