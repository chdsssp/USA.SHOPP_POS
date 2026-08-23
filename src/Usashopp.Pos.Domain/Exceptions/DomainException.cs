namespace Usashopp.Pos.Domain.Exceptions;

/// <summary>
/// Excepción base para violaciones de reglas de negocio. Se traducen a mensajes
/// claros en la UI (nunca stack traces al usuario).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensaje) : base(mensaje) { }
}
