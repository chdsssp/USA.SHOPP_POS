using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Estado del usuario autenticado en la app. Se registra como singleton y la capa de
/// presentación lo actualiza al iniciar/cerrar sesión.
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private HashSet<string> _permisos = new();

    public Guid? UsuarioId { get; private set; }
    public string? Nombre { get; private set; }

    public bool TienePermiso(string clave) => _permisos.Contains(clave);

    public void IniciarSesion(Guid usuarioId, string nombre, IEnumerable<string> permisos)
    {
        UsuarioId = usuarioId;
        Nombre = nombre;
        _permisos = new HashSet<string>(permisos);
    }

    public void CerrarSesion()
    {
        UsuarioId = null;
        Nombre = null;
        _permisos = new HashSet<string>();
    }
}
