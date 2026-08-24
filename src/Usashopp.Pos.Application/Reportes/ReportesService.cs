using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Reportes;

public record TopProductoDto(string Descripcion, int Cantidad, decimal Importe);

public record VentasPorMetodoDto(string Metodo, int NumPagos, decimal Total);

public record VentasPorUsuarioDto(string Usuario, int NumVentas, decimal Total);

public record ReporteResumenDto(
    decimal VentasTotal,
    int NumVentas,
    decimal TicketPromedio,
    int ProductosBajoStock,
    IReadOnlyList<TopProductoDto> TopProductos,
    IReadOnlyList<VentasPorMetodoDto> PorMetodoPago,
    IReadOnlyList<VentasPorUsuarioDto> PorUsuario);

/// <summary>Indicadores (KPIs) del negocio para un rango de fechas.</summary>
public class ReportesService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly IUsuarioRepository _usuarios;

    public ReportesService(IVentaRepository ventas, IVarianteRepository variantes, IUsuarioRepository usuarios)
    {
        _ventas = ventas;
        _variantes = variantes;
        _usuarios = usuarios;
    }

    public async Task<ReporteResumenDto> ObtenerAsync(DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        var ventas = (await _ventas.ListarPorFechaAsync(desde, hasta, ct))
            .Where(v => v.Estado != EstadoVenta.Cancelada)
            .ToList();

        var total = ventas.Sum(v => v.Total.Monto);
        var num = ventas.Count;
        var ticket = num > 0 ? total / num : 0m;

        var top = ventas
            .SelectMany(v => v.Detalles)
            .GroupBy(d => d.Descripcion)
            .Select(g => new TopProductoDto(g.Key, g.Sum(d => d.Cantidad), g.Sum(d => d.Importe.Monto)))
            .OrderByDescending(t => t.Cantidad)
            .Take(10)
            .ToList();

        var porMetodo = ventas
            .SelectMany(v => v.Pagos)
            .GroupBy(p => p.Metodo)
            .Select(g => new VentasPorMetodoDto(g.Key.ToString(), g.Count(), g.Sum(p => p.Monto.Monto)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var usuarios = (await _usuarios.ListarAsync(null, ct))
            .ToDictionary(u => u.Id, u => u.Nombre);

        var porUsuario = ventas
            .GroupBy(v => v.UsuarioId)
            .Select(g => new VentasPorUsuarioDto(
                usuarios.GetValueOrDefault(g.Key, "—"), g.Count(), g.Sum(v => v.Total.Monto)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var bajoStock = (await _variantes.ListarInventarioAsync(null, true, false, ct)).Count;

        return new ReporteResumenDto(total, num, ticket, bajoStock, top, porMetodo, porUsuario);
    }
}
