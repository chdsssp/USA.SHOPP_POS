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

    private static async Task IntentarAsync(Func<Task> accion)
    {
        try { await accion(); }
        catch { /* Se registra en logging de infraestructura; la venta ya quedó guardada. */ }
    }
}
