using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Cancela una venta y reintegra el stock de sus líneas.</summary>
public class CancelarVentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IRepository<NotaCredito> _notasCredito;
    private readonly IRepository<ConsumoNotaCredito> _consumosNota;
    private readonly IRepository<Cliente> _clientes;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public CancelarVentaService(
        IVentaRepository ventas,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        IRepository<NotaCredito> notasCredito,
        IRepository<ConsumoNotaCredito> consumosNota,
        IRepository<Cliente> clientes,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _ventas = ventas;
        _variantes = variantes;
        _movimientos = movimientos;
        _notasCredito = notasCredito;
        _consumosNota = consumosNota;
        _clientes = clientes;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
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
            // Restaura el saldo a favor consumido con notas de crédito en esta venta.
            var consumos = await _consumosNota.ListarAsync(c => c.VentaId == venta.Id, ct);
            foreach (var consumo in consumos)
            {
                var nota = await _notasCredito.ObtenerPorIdAsync(consumo.NotaCreditoId, ct);
                if (nota is not null)
                {
                    nota.Saldo = new Dinero(nota.Saldo.Monto + consumo.Monto.Monto);
                    if (nota.Estado == EstadoNotaCredito.Usada) nota.Estado = EstadoNotaCredito.Activa;
                    _notasCredito.Actualizar(nota);
                }
                _consumosNota.Eliminar(consumo);
            }

            // Revierte los puntos de lealtad otorgados por esta venta (piso en 0).
            if (venta.ClienteId is { } clienteId)
            {
                var cliente = await _clientes.ObtenerPorIdAsync(clienteId, ct);
                if (cliente is not null)
                {
                    var puntos = (int)(venta.Total.Monto / 10m);
                    if (puntos > 0)
                    {
                        cliente.Puntos = Math.Max(0, cliente.Puntos - puntos);
                        _clientes.Actualizar(cliente);
                    }
                }
            }

            venta.Cancelar();
            _ventas.Actualizar(venta);
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        await _auditoria.RegistrarAsync(
            "Cancelación de venta", $"Venta {venta.Folio} por {venta.Total.Monto:C2}", "Venta", venta.Id, ct);

        return Result.Ok();
    }
}
