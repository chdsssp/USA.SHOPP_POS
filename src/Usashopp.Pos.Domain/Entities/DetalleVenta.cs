using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Línea de una venta. La descripción y el precio unitario se "congelan" al momento
/// de la venta para que el histórico no cambie si luego se edita el producto.
/// </summary>
public class DetalleVenta : EntidadBase
{
    public Guid VentaId { get; set; }
    public Guid VarianteId { get; set; }
    public VarianteProducto? Variante { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public Dinero PrecioUnitario { get; set; } = Dinero.Cero;
    public Descuento? Descuento { get; set; }

    /// <summary>
    /// Costo unitario "congelado" al vender, para que la utilidad histórica no cambie si después
    /// cambia el costo del producto. Ventas anteriores a este campo se rellenaron con el costo vigente
    /// al migrar (mejor aproximación disponible).
    /// </summary>
    public Dinero CostoUnitario { get; set; } = Dinero.Cero;

    /// <summary>Importe de la línea: precio × cantidad − descuento.</summary>
    public Dinero Importe
    {
        get
        {
            var bruto = PrecioUnitario.Por(Cantidad);
            if (Descuento is { } d)
                return bruto.Menos(d.CalcularSobre(bruto));
            return bruto;
        }
    }
}
