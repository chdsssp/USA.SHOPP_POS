using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Common.Interfaces;

public interface IVentaRepository : IRepository<Venta>
{
    Task<Venta?> ObtenerPorFolioAsync(string folio, CancellationToken cancellationToken = default);

    /// <summary>Historial de ventas por rango de fechas (incluye líneas y pagos).</summary>
    Task<IReadOnlyList<Venta>> ListarPorFechaAsync(
        DateTime? desde, DateTime? hasta, CancellationToken cancellationToken = default);

    /// <summary>Una venta con sus líneas y pagos cargados.</summary>
    Task<Venta?> ObtenerConDetalleAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ventas de una sesión de caja (incluye pagos), para el corte.</summary>
    Task<IReadOnlyList<Venta>> ListarPorSesionAsync(Guid sesionCajaId, CancellationToken cancellationToken = default);
}

public interface ISesionCajaRepository : IRepository<SesionCaja>
{
    /// <summary>Devuelve la sesión de caja abierta actual, o null si no hay ninguna.</summary>
    Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken cancellationToken = default);

    /// <summary>Sesiones ya cerradas (con sus ventas y pagos) para el historial de cortes.</summary>
    Task<IReadOnlyList<SesionCaja>> ListarCerradasAsync(CancellationToken cancellationToken = default);
}

public interface IMovimientoInventarioRepository : IRepository<MovimientoInventario>
{
    /// <summary>Movimientos de una variante ordenados por fecha ascendente (para el kardex).</summary>
    Task<IReadOnlyList<MovimientoInventario>> ListarPorVarianteAsync(
        Guid varianteId, CancellationToken cancellationToken = default);
}

public interface IConfiguracionTiendaRepository
{
    Task<ConfiguracionTienda> ObtenerAsync(CancellationToken cancellationToken = default);
}
