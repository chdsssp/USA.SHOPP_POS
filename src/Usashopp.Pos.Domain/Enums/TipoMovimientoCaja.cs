namespace Usashopp.Pos.Domain.Enums;

/// <summary>Tipo de movimiento de efectivo en la caja (distinto de las ventas).</summary>
public enum TipoMovimientoCaja
{
    /// <summary>Entrada de efectivo a la caja (fondo adicional, ajuste positivo…).</summary>
    Ingreso = 0,
    /// <summary>Retiro de efectivo de la caja (sangría).</summary>
    Retiro = 1,
    /// <summary>Gasto menor pagado con efectivo de la caja.</summary>
    Gasto = 2,
    /// <summary>Reembolso de dinero al cliente por una devolución.</summary>
    Reembolso = 3
}
