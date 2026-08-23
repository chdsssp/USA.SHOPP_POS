namespace Usashopp.Pos.Domain.Common;

/// <summary>
/// Raíz de todas las entidades del dominio. Aporta identidad y auditoría básica.
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? ActualizadoEn { get; set; }
}
