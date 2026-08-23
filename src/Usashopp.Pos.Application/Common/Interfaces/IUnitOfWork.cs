namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>
/// Coordina la persistencia y las transacciones. Operaciones que tocan varias tablas
/// (venta + inventario + pago) deben ejecutarse dentro de una transacción.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);

    Task EjecutarEnTransaccionAsync(Func<Task> operacion, CancellationToken cancellationToken = default);
}
