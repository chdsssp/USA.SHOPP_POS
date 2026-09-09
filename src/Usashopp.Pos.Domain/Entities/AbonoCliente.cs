using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>Abono de un cliente a su cuenta de crédito (cuentas por cobrar).</summary>
public class AbonoCliente : EntidadBase
{
    public Guid ClienteId { get; set; }

    public Dinero Monto { get; set; } = Dinero.Cero;
    public MetodoPago Metodo { get; set; } = MetodoPago.Efectivo;
    public string? Nota { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
}
