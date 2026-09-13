using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Registro de consumo de una nota de crédito como forma de pago de una venta. Permite
/// restaurar el saldo a favor del cliente si esa venta se cancela.
/// </summary>
public class ConsumoNotaCredito : EntidadBase
{
    /// <summary>Nota de crédito de la que se descontó saldo.</summary>
    public Guid NotaCreditoId { get; set; }

    /// <summary>Venta que consumió el saldo.</summary>
    public Guid VentaId { get; set; }

    /// <summary>Importe descontado de la nota en esa venta.</summary>
    public Dinero Monto { get; set; } = Dinero.Cero;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
