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

    /// <summary>Descuento de la promoción automática aplicada a la línea (2×1, temporada…).</summary>
    public Dinero DescuentoPromocion { get; set; } = Dinero.Cero;
    public Guid? PromocionId { get; set; }
    /// <summary>Nombre "congelado" de la promoción (para ticket y reportes aunque luego se edite).</summary>
    public string? PromocionNombre { get; set; }

    /// <summary>Importe de la línea: precio × cantidad − descuento manual − promoción (nunca negativo).</summary>
    public Dinero Importe
    {
        get
        {
            var bruto = PrecioUnitario.Por(Cantidad);
            var neto = Descuento is { } d ? bruto.Menos(d.CalcularSobre(bruto)) : bruto;
            neto = neto.Menos(DescuentoPromocion);
            return neto.EsNegativo ? new Dinero(0m, neto.Moneda) : neto;
        }
    }
}
