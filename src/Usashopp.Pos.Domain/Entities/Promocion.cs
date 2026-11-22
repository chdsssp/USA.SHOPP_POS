using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Promoción automática (se aplica sola en el carrito durante su vigencia): rebaja %, monto por
/// pieza, N×M, N por $X o precio especial, sobre toda la tienda, una categoría, un producto o una
/// variante.
/// </summary>
public class Promocion : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public TipoPromocion Tipo { get; set; }
    public AlcancePromocion Alcance { get; set; }
    /// <summary>Id de la categoría / producto / variante según <see cref="Alcance"/> (null = toda la tienda).</summary>
    public Guid? AlcanceId { get; set; }

    /// <summary>% de rebaja, monto por pieza, precio del paquete (N por $X) o precio especial.</summary>
    public decimal Valor { get; set; }
    /// <summary>Piezas que lleva (N×M y N por $X).</summary>
    public int N { get; set; }
    /// <summary>Piezas que paga (N×M).</summary>
    public int M { get; set; }

    /// <summary>Vigencia en fecha LOCAL de la tienda, inclusiva (null = sin límite).</summary>
    public DateTime? Inicio { get; set; }
    public DateTime? Fin { get; set; }
    /// <summary>Días de la semana en que aplica, como banderas (bit 0 = domingo … bit 6 = sábado). 0 = todos.</summary>
    public int DiasSemana { get; set; }

    public bool Activa { get; set; } = true;
    /// <summary>Si dos promociones aplican a la misma prenda gana la de mayor prioridad.</summary>
    public int Prioridad { get; set; }
    /// <summary>¿Se suma a un descuento manual de la línea? Si no, el descuento manual la reemplaza.</summary>
    public bool AcumulableConManual { get; set; }

    public bool VigenteEn(DateTime fechaLocal)
    {
        if (!Activa) return false;
        if (Inicio is { } i && fechaLocal.Date < i.Date) return false;
        if (Fin is { } f && fechaLocal.Date > f.Date) return false;
        if (DiasSemana != 0 && (DiasSemana & (1 << (int)fechaLocal.DayOfWeek)) == 0) return false;
        return true;
    }
}
