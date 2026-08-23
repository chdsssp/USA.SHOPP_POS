namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Permite iniciar y cerrar la sesión del usuario actual.</summary>
public interface ISesionManager
{
    void IniciarSesion(Guid usuarioId, string nombre, IEnumerable<string> permisos);
    void CerrarSesion();
}
