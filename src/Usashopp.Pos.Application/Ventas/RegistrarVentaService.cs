using FluentValidation;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>
/// Caso de uso central: registra una venta completa (líneas, pagos, descuento de stock,
/// ticket y apertura de cajón) de forma transaccional.
/// </summary>
public class RegistrarVentaService
{
    private readonly IValidator<NuevaVentaDto> _validator;
    private readonly IVarianteRepository _variantes;
    private readonly IVentaRepository _ventas;
    private readonly ISesionCajaRepository _sesiones;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<NotaCredito> _notasCredito;
    private readonly IRepository<AbonoCliente> _abonosCliente;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly ITicketPrinter _impresora;
    private readonly ICashDrawer _cajon;

    public RegistrarVentaService(
        IValidator<NuevaVentaDto> validator,
        IVarianteRepository variantes,
        IVentaRepository ventas,
        ISesionCajaRepository sesiones,
        IMovimientoInventarioRepository movimientos,
        IConfiguracionTiendaRepository configuracion,
        IRepository<Cliente> clientes,
        IRepository<NotaCredito> notasCredito,
        IRepository<AbonoCliente> abonosCliente,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        ITicketPrinter impresora,
        ICashDrawer cajon)
    {
        _validator = validator;
        _variantes = variantes;
        _ventas = ventas;
        _sesiones = sesiones;
        _movimientos = movimientos;
        _configuracion = configuracion;
        _clientes = clientes;
        _notasCredito = notasCredito;
        _abonosCliente = abonosCliente;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _impresora = impresora;
        _cajon = cajon;
    }

    public async Task<Result<ResultadoVentaDto>> EjecutarAsync(NuevaVentaDto dto, CancellationToken ct = default)
    {
        var validacion = await _validator.ValidateAsync(dto, ct);
        if (!validacion.IsValid)
            return Result.Falla<ResultadoVentaDto>(string.Join(" ", validacion.Errors.Select(e => e.ErrorMessage)));

        if (_usuario.UsuarioId is not { } usuarioId)
            return Result.Falla<ResultadoVentaDto>("No hay un usuario autenticado.");

        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        if (sesion is null)
            return Result.Falla<ResultadoVentaDto>("No hay una sesión de caja abierta. Abre la caja antes de vender.");

        var config = await _configuracion.ObtenerAsync(ct);

        var venta = new Venta
        {
            SesionCajaId = sesion.Id,
            UsuarioId = usuarioId,
            ClienteId = dto.ClienteId,
            Fecha = _reloj.UtcAhora,
            Notas = dto.Notas
        };

        if (dto.DescuentoGlobalTipo is { } tipoGlobal)
            venta.DescuentoGlobal = new Descuento(tipoGlobal, dto.DescuentoGlobalValor);

        // Construir líneas con precio y descripción "congelados", validando stock.
        var variantesAfectadas = new List<(VarianteProducto variante, int cantidad)>();
        foreach (var lineaDto in dto.Lineas)
        {
            var variante = await _variantes.ObtenerPorIdAsync(lineaDto.VarianteId, ct);
            if (variante is null)
                return Result.Falla<ResultadoVentaDto>($"La variante {lineaDto.VarianteId} no existe.");

            if (!config.PermitirVentaStockNegativo && variante.StockActual < lineaDto.Cantidad)
                return Result.Falla<ResultadoVentaDto>(
                    $"Stock insuficiente de «{variante.DescripcionCompleta}»: disponible {variante.StockActual}, solicitado {lineaDto.Cantidad}.");

            // Precio: el capturado en el POS si viene (edición de precio), o el del catálogo.
            var precio = lineaDto.PrecioManual is { } pm && pm > 0 ? new Dinero(pm) : variante.PrecioVenta;

            var linea = new DetalleVenta
            {
                VarianteId = variante.Id,
                Descripcion = variante.DescripcionCompleta,
                Cantidad = lineaDto.Cantidad,
                PrecioUnitario = precio,
                Descuento = lineaDto.DescuentoTipo is { } t ? new Descuento(t, lineaDto.DescuentoValor) : null
            };
            venta.AgregarLinea(linea);
            variantesAfectadas.Add((variante, lineaDto.Cantidad));
        }

        // Registrar pagos y validar cobertura del total.
        foreach (var pagoDto in dto.Pagos)
            venta.RegistrarPago(new Pago
            {
                Metodo = pagoDto.Metodo,
                Monto = new Dinero(pagoDto.Monto),
                Referencia = pagoDto.Referencia,
                Fecha = _reloj.UtcAhora
            });

        if (!venta.EstaPagada)
            return Result.Falla<ResultadoVentaDto>(
                $"El pago ({venta.TotalPagado}) no cubre el total ({venta.Total}).");

        // Cliente (para crédito, nota de crédito y lealtad).
        Cliente? cliente = dto.ClienteId is { } cid ? await _clientes.ObtenerPorIdAsync(cid, ct) : null;

        // Formas de pago que requieren cliente: crédito (genera CxC) y nota de crédito (consume saldo a favor).
        var montoCredito = dto.Pagos.Where(p => p.Metodo == MetodoPago.Credito).Sum(p => p.Monto);
        var montoNota = dto.Pagos.Where(p => p.Metodo == MetodoPago.NotaCredito).Sum(p => p.Monto);
        var notasParaConsumir = new List<NotaCredito>();

        if (montoCredito > 0 || montoNota > 0)
        {
            if (cliente is null)
                return Result.Falla<ResultadoVentaDto>("Selecciona un cliente para pagar con crédito o nota de crédito.");

            if (montoCredito > 0)
            {
                var disponible = cliente.LimiteCredito.Monto - await SaldoCreditoAsync(cliente.Id, ct);
                if (montoCredito > disponible)
                    return Result.Falla<ResultadoVentaDto>($"El crédito excede el disponible del cliente ({disponible:C2}).");
            }

            if (montoNota > 0)
            {
                notasParaConsumir = (await _notasCredito.ListarAsync(
                        n => n.ClienteId == cliente.Id && n.Estado == EstadoNotaCredito.Activa, ct))
                    .OrderBy(n => n.Fecha).ToList();
                var saldoNotas = notasParaConsumir.Sum(n => n.Saldo.Monto);
                if (montoNota > saldoNotas)
                    return Result.Falla<ResultadoVentaDto>($"La nota de crédito no cubre ese monto (disponible {saldoNotas:C2}).");
            }
        }

        venta.Folio = $"{config.PrefijoFolioVenta}{config.ConsecutivoVenta:D6}";
        venta.MarcarPagada();

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            await _ventas.AgregarAsync(venta, ct);

            foreach (var (variante, cantidad) in variantesAfectadas)
            {
                variante.AplicarCambioStock(-cantidad);
                _variantes.Actualizar(variante);

                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.Venta,
                    Cantidad = -cantidad,
                    ReferenciaId = venta.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }

            // Consumir el saldo a favor (notas de crédito) usado como pago, FIFO.
            var restanteNota = montoNota;
            foreach (var nota in notasParaConsumir)
            {
                if (restanteNota <= 0) break;
                var aplica = Math.Min(restanteNota, nota.Saldo.Monto);
                nota.Saldo = new Dinero(nota.Saldo.Monto - aplica);
                if (nota.Saldo.Monto <= 0) nota.Estado = EstadoNotaCredito.Usada;
                _notasCredito.Actualizar(nota);
                restanteNota -= aplica;
            }

            // Lealtad: 1 punto por cada $10 del total de la venta.
            if (cliente is not null)
            {
                var puntos = (int)(venta.Total.Monto / 10m);
                if (puntos > 0)
                {
                    cliente.Puntos += puntos;
                    _clientes.Actualizar(cliente);
                }
            }

            config.ConsecutivoVenta++;

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        // Hardware: no debe tumbar la venta ya registrada si falla.
        if (dto.Imprimir)
            await IntentarAsync(() => _impresora.ImprimirVentaAsync(venta, ct));

        var pagoEnEfectivo = dto.Pagos.Any(p => p.Metodo == MetodoPago.Efectivo);
        if (dto.AbrirCajon && pagoEnEfectivo)
            await IntentarAsync(() => _cajon.AbrirAsync(ct));

        return Result.Ok(new ResultadoVentaDto(venta.Id, venta.Folio, venta.Total.Monto, venta.Cambio.Monto));
    }

    /// <summary>Saldo de crédito actual del cliente: cargos a crédito menos abonos.</summary>
    private async Task<decimal> SaldoCreditoAsync(Guid clienteId, CancellationToken ct)
    {
        var ventas = await _ventas.ListarPorClienteAsync(clienteId, ct);
        var cargos = ventas.SelectMany(v => v.Pagos)
            .Where(p => p.Metodo == MetodoPago.Credito).Sum(p => p.Monto.Monto);
        var abonos = (await _abonosCliente.ListarAsync(a => a.ClienteId == clienteId, ct)).Sum(a => a.Monto.Monto);
        return cargos - abonos;
    }

    private static async Task IntentarAsync(Func<Task> accion)
    {
        try { await accion(); }
        catch { /* Se registra en logging de infraestructura; la venta ya quedó guardada. */ }
    }
}
