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

    /// <summary>Se puede recibir mercancía mientras haya líneas pendientes y no esté cancelada.</summary>
    public bool PuedeRecibir =>
        Estado is EstadoCompra.Borrador or EstadoCompra.Ordenada or EstadoCompra.RecibidaParcial
        && Detalles.Any(d => d.Pendiente > 0);

    public void MarcarOrdenada() => Estado = EstadoCompra.Ordenada;
    public void MarcarRecibida() => Estado = EstadoCompra.Recibida;
    public void Cancelar() => Estado = EstadoCompra.Cancelada;

    /// <summary>Recalcula el estado a partir de lo recibido en las líneas.</summary>
    public void RecalcularRecepcion()
    {
        var pedido = Detalles.Sum(d => d.Cantidad);
        var recibido = Detalles.Sum(d => d.CantidadRecibida);
        Estado = recibido <= 0
            ? EstadoCompra.Ordenada
            : recibido >= pedido ? EstadoCompra.Recibida : EstadoCompra.RecibidaParcial;
    }
}
