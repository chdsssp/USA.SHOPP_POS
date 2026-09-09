using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>Abono/pago realizado a una compra (cuentas por pagar al proveedor).</summary>
public class PagoCompra : EntidadBase
{
    public Guid CompraId { get; set; }

    public Dinero Monto { get; set; } = Dinero.Cero;
    public MetodoPago Metodo { get; set; } = MetodoPago.Efectivo;
    public string? Nota { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
}
