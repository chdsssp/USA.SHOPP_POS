namespace Usashopp.Pos.Domain.Exceptions;

public sealed class CajaNoAbiertaException : DomainException
{
    public CajaNoAbiertaException()
        : base("No hay una sesión de caja abierta. Abre la caja antes de registrar ventas.") { }
}
