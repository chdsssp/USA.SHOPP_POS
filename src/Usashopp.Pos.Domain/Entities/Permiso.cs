using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Permiso granular identificado por una clave estable (p. ej. "ventas.crear",
/// "ventas.cancelar", "descuentos.aplicar", "caja.corte", "config.editar").
/// </summary>
public class Permiso : EntidadBase
{
    public string Clave { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Rol> Roles { get; set; } = new List<Rol>();
}
