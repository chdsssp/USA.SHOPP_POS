using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

public class DetalleCompra : EntidadBase
{
    public Guid CompraId { get; set; }
    public Guid VarianteId { get; set; }
    public VarianteProducto? Variante { get; set; }

    public int Cantidad { get; set; }
    public Dinero CostoUnitario { get; set; } = Dinero.Cero;

    public Dinero Importe => CostoUnitario.Por(Cantidad);
}
