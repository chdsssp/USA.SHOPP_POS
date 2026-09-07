using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Nota de crédito: saldo a favor de un cliente emitido por la devolución de una venta.
/// Conserva el importe original y el saldo disponible (el canje como forma de pago se
/// implementa en un lote posterior).
/// </summary>
public class NotaCredito : EntidadBase
{
    public string Folio { get; set; } = string.Empty;

    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    /// <summary>Venta que originó la devolución.</summary>
    public Guid VentaId { get; set; }

    /// <summary>Importe emitido originalmente.</summary>
    public Dinero Monto { get; set; } = Dinero.Cero;

    /// <summary>Saldo aún disponible para usar.</summary>
    public Dinero Saldo { get; set; } = Dinero.Cero;

    public EstadoNotaCredito Estado { get; set; } = EstadoNotaCredito.Activa;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
}
