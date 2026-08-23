using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Fuente de la verdad del stock. Cada cambio de existencias (venta, compra, ajuste,
/// devolución, merma) genera un movimiento.
/// </summary>
public class MovimientoInventario : EntidadBase
{
    public Guid VarianteId { get; set; }
    public VarianteProducto? Variante { get; set; }

    public TipoMovimientoInventario Tipo { get; set; }

    /// <summary>Cantidad con signo (+ ingresa stock, − lo reduce).</summary>
    public int Cantidad { get; set; }

    public string? Motivo { get; set; }

    /// <summary>Id del documento que originó el movimiento (venta, compra, ajuste…).</summary>
    public Guid? ReferenciaId { get; set; }

    public Guid UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
