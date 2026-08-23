namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Usuario autenticado en la sesión actual de la app.</summary>
public interface ICurrentUser
{
    Guid? UsuarioId { get; }
    string? Nombre { get; }

    bool TienePermiso(string clave);
}
