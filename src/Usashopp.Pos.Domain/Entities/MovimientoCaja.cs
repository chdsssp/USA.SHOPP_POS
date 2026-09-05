using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Movimiento de efectivo de la caja que no es una venta: ingreso, retiro (sangría),
/// gasto menor o reembolso. Afecta el efectivo esperado del corte.
/// </summary>
public class MovimientoCaja : EntidadBase
{
    public Guid SesionCajaId { get; set; }

    public TipoMovimientoCaja Tipo { get; set; }

    /// <summary>Importe siempre positivo; el <see cref="Tipo"/> define si suma o resta.</summary>
    public Dinero Monto { get; set; } = Dinero.Cero;

    public string? Concepto { get; set; }

    public Guid UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Los ingresos suman al efectivo; retiros, gastos y reembolsos restan.</summary>
    public bool EsEntrada => Tipo == TipoMovimientoCaja.Ingreso;

    /// <summary>Efecto con signo sobre el efectivo de la caja.</summary>
    public decimal Efecto => EsEntrada ? Monto.Monto : -Monto.Monto;
}
