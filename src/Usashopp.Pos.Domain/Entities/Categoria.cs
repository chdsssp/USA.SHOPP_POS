using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

public class Categoria : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
