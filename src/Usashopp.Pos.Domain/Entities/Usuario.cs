using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

public class Usuario : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string UsuarioLogin { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña o PIN (nunca en texto plano).</summary>
    public string HashContrasena { get; set; } = string.Empty;

    /// <summary>Hash del PIN numérico (4–6 dígitos) para autorizar y cambiar de cajero rápido. Opcional.</summary>
    public string? PinHash { get; set; }

    public bool Activo { get; set; } = true;

    public Guid RolId { get; set; }
    public Rol? Rol { get; set; }
}
