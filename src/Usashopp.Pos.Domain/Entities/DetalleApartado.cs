using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

public class DetalleApartado : EntidadBase
{
    public Guid ApartadoId { get; set; }
    public Guid VarianteId { get; set; }
    public VarianteProducto? Variante { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public Dinero PrecioUnitario { get; set; } = Dinero.Cero;

    public Dinero Importe => PrecioUnitario.Por(Cantidad);
}
