namespace Usashopp.Pos.Domain.Exceptions;

public sealed class StockInsuficienteException : DomainException
{
    public StockInsuficienteException(string descripcionVariante, int disponible, int solicitado)
        : base($"Stock insuficiente de «{descripcionVariante}»: disponible {disponible}, solicitado {solicitado}.")
    {
        Disponible = disponible;
        Solicitado = solicitado;
    }

    public int Disponible { get; }
    public int Solicitado { get; }
}
