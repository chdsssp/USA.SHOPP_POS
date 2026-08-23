using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Pago aplicado a una venta. Una venta puede tener varios pagos (pago mixto).
/// </summary>
public class Pago : EntidadBase
{
    public Guid VentaId { get; set; }
    public MetodoPago Metodo { get; set; }
    public Dinero Monto { get; set; } = Dinero.Cero;

    /// <summary>Referencia opcional (autorización de tarjeta, folio de transferencia…).</summary>
    public string? Referencia { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
