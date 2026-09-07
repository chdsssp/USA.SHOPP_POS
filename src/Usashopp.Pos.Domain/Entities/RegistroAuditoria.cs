using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Asiento de la bitácora de auditoría: una acción relevante realizada por un usuario
/// (inicio de sesión, cancelación, devolución, movimientos de caja, altas de usuario…).
/// Conserva el nombre del usuario para que el registro siga siendo legible aunque el
/// usuario se elimine después.
/// </summary>
public class RegistroAuditoria : EntidadBase
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Guid? UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;

    /// <summary>Acción realizada, en texto legible (p. ej. "Cancelación de venta").</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>Detalle con los datos concretos (folio, importe, etc.).</summary>
    public string? Detalle { get; set; }

    /// <summary>Tipo de entidad afectada (p. ej. "Venta"), opcional.</summary>
    public string? Entidad { get; set; }

    /// <summary>Id de la entidad afectada, opcional.</summary>
    public Guid? EntidadId { get; set; }
}
