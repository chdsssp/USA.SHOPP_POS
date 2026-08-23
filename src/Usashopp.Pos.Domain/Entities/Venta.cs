using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Venta (ticket). Agrupa líneas y pagos; calcula sus totales a partir de ellos.
/// </summary>
public class Venta : EntidadBase
{
    public string Folio { get; set; } = string.Empty;

    public Guid SesionCajaId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoVenta Estado { get; private set; } = EstadoVenta.EnProceso;

    public Descuento? DescuentoGlobal { get; set; }
    public string? Notas { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    // --- Totales calculados ---

    public Dinero Subtotal =>
        Detalles.Aggregate(Dinero.Cero, (acc, d) => acc.Mas(d.Importe));

    public Dinero TotalDescuentoGlobal =>
        DescuentoGlobal is { } d ? d.CalcularSobre(Subtotal) : Dinero.Cero;

    /// <summary>Total a cobrar. El IVA se asume incluido en el precio (configurable).</summary>
    public Dinero Total => Subtotal.Menos(TotalDescuentoGlobal);

    public Dinero TotalPagado =>
        Pagos.Aggregate(Dinero.Cero, (acc, p) => acc.Mas(p.Monto));

    public Dinero Cambio
    {
        get
        {
            var diferencia = TotalPagado.Menos(Total);
            return diferencia.EsNegativo ? Dinero.Cero : diferencia;
        }
    }

    public bool EstaPagada => TotalPagado.Monto >= Total.Monto;

    // --- Comportamiento ---

    public void AgregarLinea(DetalleVenta linea)
    {
        if (linea.Cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(linea));

        var existente = Detalles.FirstOrDefault(d => d.VarianteId == linea.VarianteId && d.Descuento is null);
        if (existente is not null && linea.Descuento is null)
            existente.Cantidad += linea.Cantidad;
        else
            Detalles.Add(linea);
    }

    public void RegistrarPago(Pago pago) => Pagos.Add(pago);

    public void MarcarPagada() => Estado = EstadoVenta.Pagada;
    public void Cancelar() => Estado = EstadoVenta.Cancelada;
}
