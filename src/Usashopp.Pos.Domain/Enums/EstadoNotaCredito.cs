namespace Usashopp.Pos.Domain.Enums;

/// <summary>Estado del ciclo de vida de una nota de crédito (saldo a favor del cliente).</summary>
public enum EstadoNotaCredito
{
    /// <summary>Con saldo disponible por usar.</summary>
    Activa = 0,
    /// <summary>Ya se consumió todo el saldo.</summary>
    Usada = 1,
    /// <summary>Anulada; su saldo dejó de ser válido.</summary>
    Cancelada = 2
}
