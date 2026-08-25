using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Reportes;

public record TopProductoDto(string Descripcion, int Cantidad, decimal Importe);
public record VentasPorMetodoDto(string Metodo, int NumPagos, decimal Total);
public record VentasPorUsuarioDto(string Usuario, int NumVentas, decimal Total);
public record VentasPorCategoriaDto(string Categoria, int Unidades, decimal Importe);
public record VentasPorHoraDto(string Franja, int NumVentas, decimal Total);
public record ProductoSinMovimientoDto(string Producto, string Sku, int Stock);

public record ReporteResumenDto(
    decimal VentasTotal,
    int NumVentas,
    decimal TicketPromedio,
    int ProductosBajoStock,
    // Utilidad
    decimal CostoTotal,
    decimal UtilidadBruta,
    decimal MargenPct,
    // Descuentos y devoluciones
    decimal DescuentosOtorgados,
    int NumDevoluciones,
    decimal TotalDevoluciones,
    // Inventario valorizado
    decimal InventarioValorCosto,
    decimal InventarioValorPrecio,
    int InventarioUnidades,
    // Comparativo con el periodo anterior de igual duración
    decimal VentasPeriodoAnterior,
    decimal VariacionPct,
    // Desgloses
    IReadOnlyList<TopProductoDto> TopProductos,
    IReadOnlyList<VentasPorMetodoDto> PorMetodoPago,
    IReadOnlyList<VentasPorUsuarioDto> PorUsuario,
    IReadOnlyList<VentasPorCategoriaDto> PorCategoria,
    IReadOnlyList<VentasPorHoraDto> PorHora,
    IReadOnlyList<ProductoSinMovimientoDto> SinMovimiento);

/// <summary>Indicadores (KPIs) y desgloses del negocio para un rango de fechas.</summary>
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
        var todas = await _ventas.ListarPorFechaAsync(desde, hasta, ct);
        var ventas = todas.Where(v => v.Estado != EstadoVenta.Cancelada).ToList();

        // Inventario (todas las variantes, para costos, categorías y valorización).
        var inventario = await _variantes.ListarInventarioAsync(null, false, true, ct);
        var costoPorVariante = inventario
            .GroupBy(v => v.VarianteId)
            .ToDictionary(g => g.Key, g => g.First().Costo);
        var categoriaPorVariante = inventario
            .GroupBy(v => v.VarianteId)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.First().Categoria) ? "Sin categoría" : g.First().Categoria!);
        var activas = inventario.Where(v => v.Activo).ToList();

        // --- KPIs de venta ---
        var total = ventas.Sum(v => v.Total.Monto);
        var num = ventas.Count;
        var ticket = num > 0 ? total / num : 0m;

        // --- Utilidad / margen (costo actual de la variante como aproximación) ---
        var costoTotal = ventas
            .SelectMany(v => v.Detalles)
            .Sum(d => d.Cantidad * costoPorVariante.GetValueOrDefault(d.VarianteId, 0m));
        var utilidad = total - costoTotal;
        var margen = total > 0 ? utilidad / total : 0m;

        // --- Descuentos otorgados (por línea + global) ---
        var descLinea = ventas
            .SelectMany(v => v.Detalles)
            .Sum(d => (d.PrecioUnitario.Monto * d.Cantidad) - d.Importe.Monto);
        var descGlobal = ventas.Sum(v => v.TotalDescuentoGlobal.Monto);
        var descuentos = descLinea + descGlobal;

        // --- Devoluciones y cancelaciones ---
        var devoluciones = todas
            .Where(v => v.Estado is EstadoVenta.Cancelada or EstadoVenta.Devuelta or EstadoVenta.ParcialmenteDevuelta)
            .ToList();

        // --- Inventario valorizado ---
        var valorCosto = activas.Sum(v => v.Stock * v.Costo);
        var valorPrecio = activas.Sum(v => v.Stock * v.Precio);
        var unidades = activas.Sum(v => v.Stock);
        var bajoStock = activas.Count(v => v.BajoStock);

        // --- Top productos ---
        var top = ventas
            .SelectMany(v => v.Detalles)
            .GroupBy(d => d.Descripcion)
            .Select(g => new TopProductoDto(g.Key, g.Sum(d => d.Cantidad), g.Sum(d => d.Importe.Monto)))
            .OrderByDescending(t => t.Cantidad)
            .Take(10)
            .ToList();

        // --- Por forma de pago ---
        var porMetodo = ventas
            .SelectMany(v => v.Pagos)
            .GroupBy(p => p.Metodo)
            .Select(g => new VentasPorMetodoDto(g.Key.ToString(), g.Count(), g.Sum(p => p.Monto.Monto)))
            .OrderByDescending(x => x.Total)
            .ToList();

        // --- Por usuario ---
        var usuarios = (await _usuarios.ListarAsync(null, ct)).ToDictionary(u => u.Id, u => u.Nombre);
        var porUsuario = ventas
            .GroupBy(v => v.UsuarioId)
            .Select(g => new VentasPorUsuarioDto(usuarios.GetValueOrDefault(g.Key, "—"), g.Count(), g.Sum(v => v.Total.Monto)))
            .OrderByDescending(x => x.Total)
            .ToList();

        // --- Por categoría ---
        var porCategoria = ventas
            .SelectMany(v => v.Detalles)
            .GroupBy(d => categoriaPorVariante.GetValueOrDefault(d.VarianteId, "Sin categoría"))
            .Select(g => new VentasPorCategoriaDto(g.Key, g.Sum(d => d.Cantidad), g.Sum(d => d.Importe.Monto)))
            .OrderByDescending(x => x.Importe)
            .ToList();

        // --- Por hora del día ---
        var porHora = ventas
            .GroupBy(v => v.Fecha.Hour)
            .Select(g => new VentasPorHoraDto($"{g.Key:00}:00", g.Count(), g.Sum(v => v.Total.Monto)))
            .OrderBy(x => x.Franja)
            .ToList();

        // --- Productos sin movimiento en el periodo ---
        var vendidas = ventas.SelectMany(v => v.Detalles).Select(d => d.VarianteId).ToHashSet();
        var sinMovimiento = activas
            .Where(v => !vendidas.Contains(v.VarianteId))
            .OrderByDescending(v => v.Stock)
            .Take(30)
            .Select(v => new ProductoSinMovimientoDto(
                $"{v.Producto}{(string.IsNullOrWhiteSpace(v.Talla) ? "" : $" · {v.Talla}")}{(string.IsNullOrWhiteSpace(v.Color) ? "" : $" {v.Color}")}",
                v.Sku, v.Stock))
            .ToList();

        // --- Comparativo con el periodo anterior de igual duración ---
        var totalAnterior = 0m;
        var variacion = 0m;
        if (desde is { } d0 && hasta is { } h0 && h0 > d0)
        {
            var duracion = h0 - d0;
            var anteriores = await _ventas.ListarPorFechaAsync(d0 - duracion, d0.AddTicks(-1), ct);
            totalAnterior = anteriores.Where(v => v.Estado != EstadoVenta.Cancelada).Sum(v => v.Total.Monto);
            variacion = totalAnterior > 0 ? (total - totalAnterior) / totalAnterior : (total > 0 ? 1m : 0m);
        }

        return new ReporteResumenDto(
            total, num, ticket, bajoStock,
            costoTotal, utilidad, margen,
            descuentos, devoluciones.Count, devoluciones.Sum(v => v.Total.Monto),
            valorCosto, valorPrecio, unidades,
            totalAnterior, variacion,
            top, porMetodo, porUsuario, porCategoria, porHora, sinMovimiento);
    }
}
