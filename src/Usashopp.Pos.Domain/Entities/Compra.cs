using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Compra a proveedor. Al recibirse, ingresa stock y puede actualizar el costo de las variantes.
/// </summary>
public class Compra : EntidadBase
{
    public string Folio { get; set; } = string.Empty;

    public Guid ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoCompra Estado { get; private set; } = EstadoCompra.Borrador;

    public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();

    public Dinero Total =>
        Detalles.Aggregate(Dinero.Cero, (acc, d) => acc.Mas(d.Importe));

    public void MarcarRecibida() => Estado = EstadoCompra.Recibida;
    public void Cancelar() => Estado = EstadoCompra.Cancelada;
}
