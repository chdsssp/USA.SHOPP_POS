using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IVentaSuspendidaRepository : IRepository<VentaSuspendida>
{
    /// <summary>Ventas en espera (o borradores) con sus líneas; opcionalmente de un usuario.</summary>
    Task<IReadOnlyList<VentaSuspendida>> ListarConLineasAsync(bool esBorrador, Guid? usuarioId, CancellationToken cancellationToken = default);

    Task<VentaSuspendida?> ObtenerConLineasAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(bool esBorrador, CancellationToken cancellationToken = default);
}
