namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Abstracción del reloj para poder testear lógica que depende de la fecha/hora.</summary>
public interface IDateTime
{
    DateTime Ahora { get; }
    DateTime UtcAhora { get; }
}
