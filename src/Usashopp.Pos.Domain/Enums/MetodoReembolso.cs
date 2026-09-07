namespace Usashopp.Pos.Domain.Enums;

/// <summary>Forma en que se reembolsa el importe de una devolución.</summary>
public enum MetodoReembolso
{
    /// <summary>Efectivo de la caja abierta (registra un movimiento de caja de tipo Reembolso).</summary>
    Efectivo = 0,
    /// <summary>Saldo a favor del cliente (emite una nota de crédito; no toca la caja).</summary>
    NotaCredito = 1
}
