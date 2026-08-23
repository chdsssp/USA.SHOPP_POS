using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    /// <summary>Usuario por login con su rol y permisos cargados.</summary>
    Task<Usuario?> ObtenerPorLoginAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>Usuarios activos con su rol cargado.</summary>
    Task<IReadOnlyList<Usuario>> ListarConRolAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteLoginAsync(string login, Guid? exceptoId = null, CancellationToken cancellationToken = default);
}

public interface IRolRepository : IRepository<Rol>
{
    Task<IReadOnlyList<Rol>> ListarConPermisosAsync(CancellationToken cancellationToken = default);
}
