namespace Usashopp.Pos.Infrastructure.Persistence.Seed;

/// <summary>
/// La base no se pudo preparar al arrancar. <see cref="RespaldoPrevio"/> es el respaldo creado justo
/// antes de migrar (si lo hay), para ofrecer restaurarlo.
/// </summary>
public sealed class ErrorInicializacionException : Exception
{
    public string? RespaldoPrevio { get; }

    public ErrorInicializacionException(string mensaje, string? respaldoPrevio, Exception interna)
        : base(mensaje, interna) => RespaldoPrevio = respaldoPrevio;
}
