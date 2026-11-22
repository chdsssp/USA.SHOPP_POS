using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Carrito puesto "en espera" (o el borrador del carrito activo) guardado en la base para que no se
/// pierda si la app se cierra, se traba o se va la luz. No mueve stock: es solo una nota de lo que el
/// cliente lleva; al recuperarse se revalidan precio y existencia.
/// </summary>
public class VentaSuspendida : EntidadBase
{
    public Guid UsuarioId { get; set; }
    public Guid? SesionCajaId { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public Guid? ClienteId { get; set; }
    public Descuento? DescuentoGlobal { get; set; }
    public string? Notas { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// true = borrador automático del carrito activo de ese usuario (uno por usuario), para
    /// recuperarlo tras un cierre inesperado. false = venta puesta en espera por el cajero.
    /// </summary>
    public bool EsBorrador { get; set; }

    public ICollection<LineaVentaSuspendida> Lineas { get; set; } = new List<LineaVentaSuspendida>();
}

public class LineaVentaSuspendida : EntidadBase
{
    public Guid VentaSuspendidaId { get; set; }
    public Guid VarianteId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    /// <summary>Precio de catálogo al suspender (para detectar cambios al recuperar).</summary>
    public Dinero PrecioUnitario { get; set; } = Dinero.Cero;
    /// <summary>Precio con el que se iba a cobrar (puede estar editado).</summary>
    public Dinero PrecioVenta { get; set; } = Dinero.Cero;
    public int Cantidad { get; set; }
    public Descuento? Descuento { get; set; }
    public int Orden { get; set; }
}
