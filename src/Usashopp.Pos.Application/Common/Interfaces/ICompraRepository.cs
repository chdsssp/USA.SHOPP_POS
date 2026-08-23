using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface ICompraRepository : IRepository<Compra>
{
    /// <summary>Listado de compras con proveedor y líneas cargados.</summary>
    Task<IReadOnlyList<Compra>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Compra con proveedor, líneas y sus variantes/productos cargados.</summary>
    Task<Compra?> ObtenerConDetalleAsync(Guid id, CancellationToken cancellationToken = default);
}
