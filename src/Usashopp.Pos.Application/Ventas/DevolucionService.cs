using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>
/// Devolución (parcial o total) de mercancía de una venta: reintegra el stock de las
/// variantes devueltas mediante movimientos de inventario. No modifica el importe de la
/// venta (el reembolso del dinero se maneja aparte); actualiza el estado de la venta.
/// </summary>
public class DevolucionService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public DevolucionService(
        IVentaRepository ventas,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _ventas = ventas;
        _variantes = variantes;
        _movimientos = movimientos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    /// <summary>Líneas devolvibles de una venta (agrupadas por variante).</summary>
    public async Task<IReadOnlyList<DevolucionLineaDto>> ObtenerLineasAsync(Guid ventaId, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return Array.Empty<DevolucionLineaDto>();

        var devueltoPorVariante = await ObtenerDevueltoPorVarianteAsync(ventaId, ct);

        return venta.Detalles
            .GroupBy(d => d.VarianteId)
            .Select(g =>
            {
                var vendida = g.Sum(d => d.Cantidad);
                var devuelta = devueltoPorVariante.GetValueOrDefault(g.Key, 0);
                var primera = g.First();
                return new DevolucionLineaDto(
                    g.Key, primera.Descripcion, primera.PrecioUnitario.Monto,
                    vendida, devuelta, Math.Max(0, vendida - devuelta));
            })
            .ToList();
    }

    public async Task<Result> EjecutarAsync(Guid ventaId, IReadOnlyList<DevolucionItemDto> items, CancellationToken ct = default)
    {
        var solicitados = items.Where(i => i.Cantidad > 0).ToList();
        if (solicitados.Count == 0)
            return Result.Falla("Indica al menos una cantidad a devolver.");

        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return Result.Falla("La venta no existe.");
        if (venta.Estado == EstadoVenta.Cancelada) return Result.Falla("La venta está cancelada.");
        if (venta.Estado == EstadoVenta.Devuelta) return Result.Falla("La venta ya fue devuelta por completo.");

        var vendidoPorVariante = venta.Detalles
            .GroupBy(d => d.VarianteId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));
        var devueltoPorVariante = await ObtenerDevueltoPorVarianteAsync(ventaId, ct);

        // Validaciones antes de tocar nada.
        foreach (var item in solicitados)
        {
            if (!vendidoPorVariante.TryGetValue(item.VarianteId, out var vendida))
                return Result.Falla("Una de las variantes no pertenece a esta venta.");
            var disponible = vendida - devueltoPorVariante.GetValueOrDefault(item.VarianteId, 0);
            if (item.Cantidad > disponible)
                return Result.Falla($"No puedes devolver {item.Cantidad}; disponible {disponible}.");
        }

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            foreach (var item in solicitados)
            {
                var variante = await _variantes.ObtenerPorIdAsync(item.VarianteId, ct);
                if (variante is null) continue;

                variante.AplicarCambioStock(item.Cantidad); // reintegra stock
                _variantes.Actualizar(variante);

                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.Devolucion,
                    Cantidad = item.Cantidad,
                    Motivo = $"Devolución venta {venta.Folio}",
                    ReferenciaId = venta.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }

            // Estado: total si ya no queda nada por devolver, parcial en otro caso.
            var totalVendido = vendidoPorVariante.Values.Sum();
            var totalDevuelto = devueltoPorVariante.Values.Sum() + solicitados.Sum(i => i.Cantidad);
            if (totalDevuelto >= totalVendido)
                venta.MarcarDevuelta();
            else
                venta.MarcarParcialmenteDevuelta();
            _ventas.Actualizar(venta);

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok();
    }

    /// <summary>Cantidad ya devuelta por variante (suma de movimientos de devolución de la venta).</summary>
    private async Task<Dictionary<Guid, int>> ObtenerDevueltoPorVarianteAsync(Guid ventaId, CancellationToken ct)
    {
        var movimientos = await _movimientos.ListarPorReferenciaAsync(ventaId, ct);
        return movimientos
            .Where(m => m.Tipo == TipoMovimientoInventario.Devolucion)
            .GroupBy(m => m.VarianteId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Cantidad));
    }
}
