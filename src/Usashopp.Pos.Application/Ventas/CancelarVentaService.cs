using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Cancela una venta y reintegra el stock de sus líneas.</summary>
public class CancelarVentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public CancelarVentaService(
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

    public async Task<Result> EjecutarAsync(Guid ventaId, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return Result.Falla("La venta no existe.");
        if (venta.Estado == EstadoVenta.Cancelada) return Result.Falla("La venta ya está cancelada.");
        if (venta.Estado is EstadoVenta.Devuelta or EstadoVenta.ParcialmenteDevuelta)
            return Result.Falla("La venta tiene devoluciones registradas; no se puede cancelar.");

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            foreach (var d in venta.Detalles)
            {
                var variante = await _variantes.ObtenerPorIdAsync(d.VarianteId, ct);
                if (variante is null) continue;
                variante.AplicarCambioStock(d.Cantidad); // reintegra el stock
                _variantes.Actualizar(variante);
                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.Devolucion,
                    Cantidad = d.Cantidad,
                    Motivo = $"Cancelación venta {venta.Folio}",
                    ReferenciaId = venta.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }
            venta.Cancelar();
            _ventas.Actualizar(venta);
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok();
    }
}
