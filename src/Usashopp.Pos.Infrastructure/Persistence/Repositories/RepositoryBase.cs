using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Infrastructure.Persistence.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : EntidadBase
{
    protected readonly AppDbContext Db;
    protected DbSet<T> Set => Db.Set<T>();

    public RepositoryBase(AppDbContext db) => Db = db;

    public virtual Task<T?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> ListarAsync(
        Expression<Func<T, bool>>? filtro = null, CancellationToken ct = default)
    {
        IQueryable<T> query = Set;
        if (filtro is not null)
            query = query.Where(filtro);
        return await query.ToListAsync(ct);
    }

    public virtual async Task AgregarAsync(T entidad, CancellationToken ct = default) =>
        await Set.AddAsync(entidad, ct);

    public virtual void Actualizar(T entidad) => Set.Update(entidad);

    public virtual void Eliminar(T entidad) => Set.Remove(entidad);
}
