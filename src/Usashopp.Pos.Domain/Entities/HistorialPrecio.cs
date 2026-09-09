using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Asiento del historial de precios de venta de una variante: registra cada cambio
/// (precio anterior → nuevo), quién lo hizo y cuándo.
/// </summary>
public class HistorialPrecio : EntidadBase
{
    public Guid VarianteId { get; set; }

    public Dinero PrecioAnterior { get; set; } = Dinero.Cero;
    public Dinero PrecioNuevo { get; set; } = Dinero.Cero;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
}
