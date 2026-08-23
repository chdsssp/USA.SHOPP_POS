using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

public class Cliente : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
}
