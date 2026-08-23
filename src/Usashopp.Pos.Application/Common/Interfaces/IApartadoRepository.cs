using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IApartadoRepository : IRepository<Apartado>
{
    /// <summary>Listado con cliente, líneas y abonos cargados.</summary>
    Task<IReadOnlyList<Apartado>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Apartado con cliente, líneas y abonos cargados.</summary>
    Task<Apartado?> ObtenerConDetalleAsync(Guid id, CancellationToken cancellationToken = default);
}
