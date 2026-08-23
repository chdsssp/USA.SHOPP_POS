using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// La unidad que realmente se vende (una talla/color concreta). Tiene su propio SKU,
/// código de barras, precio, costo y stock.
/// </summary>
public class VarianteProducto : EntidadBase, IActivable
{
    public Guid ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public Sku Sku { get; set; }
    public CodigoBarras? CodigoBarras { get; set; }

    public string? Talla { get; set; }
    public string? Color { get; set; }

    public Dinero PrecioVenta { get; set; } = Dinero.Cero;
    public Dinero Costo { get; set; } = Dinero.Cero;

    /// <summary>
    /// Stock desnormalizado por rendimiento. La fuente de la verdad son los
    /// <see cref="MovimientoInventario"/>; solo se ajusta a través de ellos.
    /// </summary>
    public int StockActual { get; private set; }
    public int StockMinimo { get; set; }

    public bool Activo { get; set; } = true;

    public bool EstaBajoMinimo => StockActual <= StockMinimo;

    /// <summary>Descripción legible para tickets y búsquedas: "Nombre — Talla · Color".</summary>
    public string DescripcionCompleta
    {
        get
        {
            var atributos = new[] { Talla, Color }.Where(a => !string.IsNullOrWhiteSpace(a));
            var sufijo = string.Join(" · ", atributos);
            var nombre = Producto?.Nombre ?? "Producto";
            return string.IsNullOrEmpty(sufijo) ? nombre : $"{nombre} — {sufijo}";
        }
    }

    /// <summary>Aplica el efecto de un movimiento de inventario al stock desnormalizado.</summary>
    internal void AplicarCambioStock(int cantidad) => StockActual += cantidad;

    /// <summary>Recalcula el stock a partir de la suma de sus movimientos (reconstrucción).</summary>
    public void EstablecerStock(int stock) => StockActual = stock;
}
