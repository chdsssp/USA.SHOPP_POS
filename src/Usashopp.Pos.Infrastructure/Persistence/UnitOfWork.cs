using Microsoft.EntityFrameworkCore;
using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    /// <summary>
    /// Ejecuta la operación en una transacción. Si ya hay una abierta (un caso de uso que orquesta a
    /// otros, p. ej. el cambio de talla = devolución + venta), se une a ella: todo se confirma o se
    /// revierte junto.
    /// </summary>
    public async Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken ct = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            await operacion();
            return;
        }

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
