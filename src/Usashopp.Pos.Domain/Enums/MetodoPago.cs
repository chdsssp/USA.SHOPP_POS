namespace Usashopp.Pos.Domain.Enums;

public enum MetodoPago
{
    Efectivo = 0,
    Tarjeta = 1,
    Transferencia = 2,
    Vales = 3,
    /// <summary>Venta a crédito: genera saldo por cobrar al cliente.</summary>
    Credito = 4,
    /// <summary>Pago con saldo a favor del cliente (nota de crédito).</summary>
    NotaCredito = 5,
    Otro = 99
}
