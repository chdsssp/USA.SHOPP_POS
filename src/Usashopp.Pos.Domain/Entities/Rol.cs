using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

public class Rol : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Permiso> Permisos { get; set; } = new List<Permiso>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public bool TienePermiso(string clave) => Permisos.Any(p => p.Clave == clave);
}
