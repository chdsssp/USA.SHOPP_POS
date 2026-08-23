using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await operacion();
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
