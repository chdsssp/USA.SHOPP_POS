using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IVentaRepository : IRepository<Venta>
{
    Task<Venta?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default);
}

public interface ISesionCajaRepository : IRepository<SesionCaja>
{
    /// <summary>Devuelve la sesión de caja abierta actual, o null si no hay ninguna.</summary>
    Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken cancellationToken = default);
}

public interface IMovimientoInventarioRepository : IRepository<MovimientoInventario>
{
}

public interface IConfiguracionTiendaRepository
{
    Task<ConfiguracionTienda> ObtenerAsync(CancellationToken cancellationToken = default);
}
