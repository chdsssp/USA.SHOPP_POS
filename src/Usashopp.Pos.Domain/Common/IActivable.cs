namespace Usashopp.Pos.Domain.Common;

/// <summary>
/// Marca de catálogos que usan borrado lógico (no se eliminan físicamente para no
/// romper el histórico de ventas).
/// </summary>
public interface IActivable
{
    bool Activo { get; }
}
