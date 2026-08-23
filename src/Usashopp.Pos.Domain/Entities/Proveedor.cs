using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

public class Proveedor : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
}
