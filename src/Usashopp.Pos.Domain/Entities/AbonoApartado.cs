using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

public class AbonoApartado : EntidadBase
{
    public Guid ApartadoId { get; set; }
    public Dinero Monto { get; set; } = Dinero.Cero;
    public MetodoPago Metodo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
}
