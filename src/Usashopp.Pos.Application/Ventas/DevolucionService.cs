using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>
/// Devolución (parcial o total) de mercancía de una venta: reintegra el stock de las
/// variantes devueltas mediante movimientos de inventario y reembolsa el dinero en efectivo
/// registrando un <see cref="TipoMovimientoCaja.Reembolso"/> en la caja abierta. El importe
/// reembolsado es el <b>neto realmente pagado</b> por las líneas devueltas (respeta descuentos
/// de línea y el descuento global). Actualiza el estado de la venta.
/// </summary>
public class DevolucionService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ISesionCajaRepository _sesiones;
    private readonly IRepository<MovimientoCaja> _movimientosCaja;
    private readonly IRepository<NotaCredito> _notasCredito;
    private readonly IRepository<AbonoCliente> _abonosCliente;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public DevolucionService(
        IVentaRepository ventas,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        ISesionCajaRepository sesiones,
        IRepository<MovimientoCaja> movimientosCaja,
        IRepository<NotaCredito> notasCredito,
        IRepository<AbonoCliente> abonosCliente,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _ventas = ventas;
        _variantes = variantes;
        _movimientos = movimientos;
        _sesiones = sesiones;
        _movimientosCaja = movimientosCaja;
        _notasCredito = notasCredito;
        _abonosCliente = abonosCliente;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
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

    /// <summary>Cliente asociado a la venta (para precargar la nota de crédito), o null.</summary>
    public async Task<Guid?> ObtenerClienteVentaAsync(Guid ventaId, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        return venta?.ClienteId;
    }

    /// <summary>
    /// Importe que se reembolsaría por los items indicados (neto pagado, con descuentos).
    /// Para vista previa en la UI; no modifica nada.
    /// </summary>
    public async Task<decimal> CalcularReembolsoAsync(Guid ventaId, IReadOnlyList<DevolucionItemDto> items, CancellationToken ct = default)
    {
        var solicitados = items.Where(i => i.Cantidad > 0).ToList();
        if (solicitados.Count == 0) return 0m;

        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return 0m;

        return CalcularReembolso(venta, solicitados);
    }

    /// <summary>
    /// Ejecuta la devolución y reembolsa el importe neto. Según <paramref name="metodo"/>:
    /// en efectivo (exige caja abierta y registra un movimiento de caja) o como nota de crédito
    /// (saldo a favor del cliente; exige un cliente y no toca la caja). Devuelve el importe
    /// reembolsado.
    /// </summary>
    public async Task<Result<decimal>> EjecutarAsync(
        Guid ventaId,
        IReadOnlyList<DevolucionItemDto> items,
        MetodoReembolso metodo = MetodoReembolso.Efectivo,
        Guid? clienteId = null,
        CancellationToken ct = default)
    {
        var solicitados = items.Where(i => i.Cantidad > 0).ToList();
        if (solicitados.Count == 0)
            return Result.Falla<decimal>("Indica al menos una cantidad a devolver.");

        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return Result.Falla<decimal>("La venta no existe.");
        if (venta.Estado == EstadoVenta.Cancelada) return Result.Falla<decimal>("La venta está cancelada.");
        if (venta.Estado == EstadoVenta.Devuelta) return Result.Falla<decimal>("La venta ya fue devuelta por completo.");

        var vendidoPorVariante = venta.Detalles
            .GroupBy(d => d.VarianteId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));
        var devueltoPorVariante = await ObtenerDevueltoPorVarianteAsync(ventaId, ct);

        // Validaciones antes de tocar nada.
        foreach (var item in solicitados)
        {
            if (!vendidoPorVariante.TryGetValue(item.VarianteId, out var vendida))
                return Result.Falla<decimal>("Una de las variantes no pertenece a esta venta.");
            var disponible = vendida - devueltoPorVariante.GetValueOrDefault(item.VarianteId, 0);
            if (item.Cantidad > disponible)
                return Result.Falla<decimal>($"No puedes devolver {item.Cantidad}; disponible {disponible}.");
        }

        var reembolso = CalcularReembolso(venta, solicitados);

        // Parte del reembolso que corresponde a lo pagado a crédito: no sale dinero, se abona a la
        // deuda del cliente (CxC). Se prorratea según qué fracción de la venta se pagó a crédito,
        // de modo que devoluciones parciales sucesivas nunca abonen más que el crédito otorgado.
        var creditoVenta = venta.Pagos.Where(p => p.Metodo == MetodoPago.Credito).Sum(p => p.Monto.Monto);
        var totalVenta = venta.Total.Monto;
        var fraccionCredito = totalVenta > 0 ? Math.Min(1m, creditoVenta / totalVenta) : 0m;
        var aDeuda = Math.Round(reembolso * fraccionCredito, 2, MidpointRounding.AwayFromZero);
        var aReembolsar = reembolso - aDeuda; // se entrega en efectivo o como nota de crédito

        // El abono a la deuda necesita al cliente deudor (una venta a crédito siempre lo tiene).
        if (aDeuda > 0 && venta.ClienteId is null)
        {
            aReembolsar += aDeuda; // salvaguarda: sin cliente no hay deuda a la cual abonar
            aDeuda = 0m;
        }

        // Prepara la forma de reembolso del remanente (validaciones antes de tocar nada).
        SesionCaja? sesion = null;
        var clienteNota = clienteId ?? venta.ClienteId;
        if (aReembolsar > 0)
        {
            if (metodo == MetodoReembolso.Efectivo)
            {
                sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
                if (sesion is null)
                    return Result.Falla<decimal>(
                        "No hay una caja abierta; abre caja para registrar el reembolso de la devolución.");
            }
            else if (clienteNota is null)
            {
                return Result.Falla<decimal>("Selecciona un cliente para emitir la nota de crédito.");
            }
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

            // Parte pagada a crédito: se abona a la deuda del cliente (reduce su CxC), no sale dinero.
            if (aDeuda > 0 && venta.ClienteId is { } deudorId)
            {
                await _abonosCliente.AgregarAsync(new AbonoCliente
                {
                    ClienteId = deudorId,
                    Monto = new Dinero(aDeuda),
                    Metodo = MetodoPago.Otro,
                    Nota = $"Devolución venta {venta.Folio}",
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }

            // Remanente: se entrega al cliente en efectivo o como nota de crédito (saldo a favor).
            if (aReembolsar > 0)
            {
                if (metodo == MetodoReembolso.Efectivo && sesion is not null)
                {
                    // Reembolso en efectivo (afecta el efectivo esperado del corte).
                    await _movimientosCaja.AgregarAsync(new MovimientoCaja
                    {
                        SesionCajaId = sesion.Id,
                        Tipo = TipoMovimientoCaja.Reembolso,
                        Monto = new Dinero(aReembolsar),
                        Concepto = $"Reembolso devolución venta {venta.Folio}",
                        UsuarioId = usuarioId,
                        Fecha = _reloj.UtcAhora
                    }, ct);
                }
                else if (metodo == MetodoReembolso.NotaCredito && clienteNota is not null)
                {
                    // Nota de crédito: saldo a favor del cliente (no toca la caja).
                    var nota = new NotaCredito
                    {
                        ClienteId = clienteNota.Value,
                        VentaId = venta.Id,
                        Monto = new Dinero(aReembolsar),
                        Saldo = new Dinero(aReembolsar),
                        Estado = EstadoNotaCredito.Activa,
                        UsuarioId = usuarioId,
                        Fecha = _reloj.UtcAhora
                    };
                    nota.Folio = $"NC-{_reloj.UtcAhora:yyyyMMdd}-{nota.Id.ToString("N")[..6].ToUpperInvariant()}";
                    await _notasCredito.AgregarAsync(nota, ct);
                }
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

        var formaTexto = metodo == MetodoReembolso.NotaCredito ? "nota de crédito" : "efectivo";
        var detalle = aDeuda > 0
            ? $"Venta {venta.Folio}; devolución {reembolso:C2} ({aDeuda:C2} a deuda de crédito, {aReembolsar:C2} en {formaTexto})"
            : $"Venta {venta.Folio}; reembolso {reembolso:C2} en {formaTexto}";
        await _auditoria.RegistrarAsync("Devolución de mercancía", detalle, "Venta", venta.Id, ct);

        return Result.Ok(reembolso);
    }

    /// <summary>
    /// Importe neto pagado por las líneas devueltas: neto por unidad de cada variante (que ya
    /// incluye su descuento de línea) ponderado entre sus líneas, por la cantidad devuelta, y
    /// prorrateando el descuento global de la venta.
    /// </summary>
    private static decimal CalcularReembolso(Venta venta, IReadOnlyList<DevolucionItemDto> solicitados)
    {
        var subtotal = venta.Subtotal.Monto;
        if (subtotal <= 0) return 0m;

        // Fracción del subtotal que realmente se cobró tras el descuento global (≤ 1).
        var factorGlobal = venta.Total.Monto / subtotal;

        var netoPorVariante = venta.Detalles
            .GroupBy(d => d.VarianteId)
            .ToDictionary(
                g => g.Key,
                g => (Importe: g.Sum(d => d.Importe.Monto), Cantidad: g.Sum(d => d.Cantidad)));

        var total = 0m;
        foreach (var item in solicitados)
        {
            if (!netoPorVariante.TryGetValue(item.VarianteId, out var info) || info.Cantidad <= 0)
                continue;
            var netoUnitario = info.Importe / info.Cantidad;
            total += netoUnitario * item.Cantidad;
        }

        return Math.Round(total * factorGlobal, 2, MidpointRounding.AwayFromZero);
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
