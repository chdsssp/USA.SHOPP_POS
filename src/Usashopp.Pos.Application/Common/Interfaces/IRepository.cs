using System.Linq.Expressions;
using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Operaciones de persistencia comunes a cualquier entidad.</summary>
public interface IRepository<T> where T : EntidadBase
{
    Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListarAsync(
        Expression<Func<T, bool>>? filtro = null,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(T entidad, CancellationToken cancellationToken = default);

    void Actualizar(T entidad);

    void Eliminar(T entidad);
}
