using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Cambio de talla/prenda: qué regresa de una venta y qué se lleva a cambio.</summary>
public record CambioDto(
    Guid VentaOrigenId,
    IReadOnlyList<DevolucionItemDto> Devueltos,
    IReadOnlyList<NuevaLineaDto> Nuevos,
    IReadOnlyList<NuevoPagoDto>? PagosDiferencia = null,
    MetodoReembolso ReembolsoDiferencia = MetodoReembolso.Efectivo,
    Guid? ClienteId = null);

/// <summary>Cuánto vale lo que regresa, cuánto lo nuevo y la diferencia (+ cobrar, − devolver).</summary>
public record PreviewCambioDto(decimal Credito, decimal TotalNuevo)
{
    public decimal Diferencia => TotalNuevo - Credito;
    public bool HayQueCobrar => Diferencia > 0;
    public bool HayQueDevolver => Diferencia < 0;
}

public record ResultadoCambioDto(
    Guid? VentaNuevaId,
    string? FolioNuevo,
    decimal Credito,
    decimal TotalNuevo,
    decimal Cobrado,
    decimal Devuelto,
    decimal Cambio,
    IReadOnlyList<string> Advertencias);

/// <summary>
/// Cambio de talla en UN solo paso (la operación más común en ropa después de vender): devuelve las
/// prendas de la venta original (reintegra stock, sin mover dinero), registra la venta nueva pagada
/// con ese crédito y cobra o devuelve la diferencia. Todo en una transacción: si algo falla, nada
/// queda a medias. La venta nueva queda ligada a la original (<see cref="Venta.VentaOrigenId"/>).
/// </summary>
public class CambioService
{
    private readonly IVentaRepository _ventas;
    private readonly IVarianteRepository _variantes;
    private readonly DevolucionService _devolucion;
    private readonly RegistrarVentaService _registrar;
    private readonly ISesionCajaRepository _sesiones;
    private readonly IRepository<MovimientoCaja> _movimientosCaja;
    private readonly IRepository<NotaCredito> _notasCredito;
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;
    private readonly ITicketPrinter _impresora;

    public CambioService(IVentaRepository ventas, IVarianteRepository variantes, DevolucionService devolucion,
        RegistrarVentaService registrar, ISesionCajaRepository sesiones, IRepository<MovimientoCaja> movimientosCaja,
        IRepository<NotaCredito> notasCredito, IConfiguracionTiendaRepository configuracion, ICurrentUser usuario,
        IDateTime reloj, IUnitOfWork uow, IAuditoria auditoria, ITicketPrinter impresora)
    {
        _ventas = ventas;
        _variantes = variantes;
        _devolucion = devolucion;
        _registrar = registrar;
        _sesiones = sesiones;
        _movimientosCaja = movimientosCaja;
        _notasCredito = notasCredito;
        _configuracion = configuracion;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
        _impresora = impresora;
    }

    /// <summary>Valida que la venta se pueda cambiar (existe, no cancelada, dentro del plazo).</summary>
    public async Task<Result> ValidarVentaAsync(Guid ventaId, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerConDetalleAsync(ventaId, ct);
        if (venta is null) return Result.Falla("La venta no existe.");
        if (venta.Estado == EstadoVenta.Cancelada) return Result.Falla("La venta está cancelada.");
        if (venta.Estado == EstadoVenta.Devuelta) return Result.Falla("Esa venta ya se devolvió por completo.");

        var config = await _configuracion.ObtenerAsync(ct);
        if (config.DiasMaximosCambio > 0 && venta.Fecha.AddDays(config.DiasMaximosCambio) < _reloj.UtcAhora)
            return Result.Falla($"La venta tiene más de {config.DiasMaximosCambio} días; ya no aplica cambio.");
        return Result.Ok();
    }

    /// <summary>Crédito de lo devuelto (neto pagado) vs. total de lo nuevo, con las reglas del dominio.</summary>
    public async Task<PreviewCambioDto> CalcularAsync(Guid ventaId, IReadOnlyList<DevolucionItemDto> devueltos,
        IReadOnlyList<NuevaLineaDto> nuevos, CancellationToken ct = default)
    {
        var credito = await _devolucion.CalcularReembolsoAsync(ventaId, devueltos, ct);
        var lineas = new List<LineaCalculo>();
        foreach (var n in nuevos.Where(n => n.Cantidad > 0))
        {
            var v = await _variantes.ObtenerPorIdAsync(n.VarianteId, ct);
            if (v is null) continue;
            lineas.Add(new LineaCalculo(v.Id, n.Cantidad, n.PrecioManual ?? v.PrecioVenta.Monto, n.DescuentoTipo, n.DescuentoValor));
        }
        return new PreviewCambioDto(credito, CalculadoraTotales.Calcular(lineas).Total);
    }

    public async Task<Result<ResultadoCambioDto>> EjecutarAsync(CambioDto dto, CancellationToken ct = default)
    {
        var devueltos = dto.Devueltos.Where(d => d.Cantidad > 0).ToList();
        var nuevos = dto.Nuevos.Where(n => n.Cantidad > 0).ToList();
        if (devueltos.Count == 0) return Result.Falla<ResultadoCambioDto>("Indica qué prenda regresa el cliente.");

        var valida = await ValidarVentaAsync(dto.VentaOrigenId, ct);
        if (valida.EsFallo) return Result.Falla<ResultadoCambioDto>(valida.Error!);

        var origen = (await _ventas.ObtenerConDetalleAsync(dto.VentaOrigenId, ct))!;
        var clienteId = dto.ClienteId ?? origen.ClienteId;
        var preview = await CalcularAsync(dto.VentaOrigenId, devueltos, nuevos, ct);

        // Validaciones de la diferencia ANTES de tocar nada.
        var pagosDiferencia = (dto.PagosDiferencia ?? Array.Empty<NuevoPagoDto>()).Where(p => p.Monto > 0).ToList();
        if (preview.HayQueCobrar && pagosDiferencia.Sum(p => p.Monto) < preview.Diferencia)
            return Result.Falla<ResultadoCambioDto>($"Falta cobrar la diferencia de {preview.Diferencia:C2}.");

        var aDevolver = preview.HayQueDevolver ? -preview.Diferencia : 0m;
        SesionCaja? sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        if (nuevos.Count > 0 && sesion is null)
            return Result.Falla<ResultadoCambioDto>("Abre la caja para registrar el cambio.");
        if (aDevolver > 0 && dto.ReembolsoDiferencia == MetodoReembolso.Efectivo && sesion is null)
            return Result.Falla<ResultadoCambioDto>("Abre la caja para devolver la diferencia en efectivo.");
        if (aDevolver > 0 && dto.ReembolsoDiferencia == MetodoReembolso.NotaCredito && clienteId is null)
            return Result.Falla<ResultadoCambioDto>("Elige un cliente para dejarle la diferencia como saldo a favor.");

        ResultadoVentaDto? ventaNueva = null;
        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;
        try
        {
            await _uow.EjecutarEnTransaccionAsync(async () =>
            {
                // 1) Regresan las prendas: stock de vuelta, sin mover dinero.
                var dev = await _devolucion.EjecutarAsync(dto.VentaOrigenId, devueltos, MetodoReembolso.SinReembolso, clienteId, ct);
                if (dev.EsFallo) throw new CambioFallidoException(dev.Error!);

                // 2) Venta nueva pagada con el crédito (+ lo que se cobra de diferencia).
                if (nuevos.Count > 0)
                {
                    var pagos = new List<NuevoPagoDto>();
                    var creditoAplicado = Math.Min(preview.Credito, preview.TotalNuevo);
                    if (creditoAplicado > 0)
                        pagos.Add(new NuevoPagoDto(MetodoPago.CreditoCambio, creditoAplicado, $"Cambio de {origen.Folio}"));
                    pagos.AddRange(pagosDiferencia);

                    var r = await _registrar.EjecutarAsync(new NuevaVentaDto(
                        nuevos, pagos, clienteId, Notas: $"Cambio de la venta {origen.Folio}",
                        Imprimir: false, AbrirCajon: pagosDiferencia.Any(p => p.Metodo == MetodoPago.Efectivo),
                        VentaOrigenId: origen.Id), ct);
                    if (r.EsFallo) throw new CambioFallidoException(r.Error!);
                    ventaNueva = r.Valor;
                }

                // 3) Diferencia a favor del cliente: efectivo de caja o saldo a favor.
                if (aDevolver > 0)
                {
                    if (dto.ReembolsoDiferencia == MetodoReembolso.NotaCredito)
                    {
                        var nota = new NotaCredito
                        {
                            ClienteId = clienteId!.Value,
                            VentaId = origen.Id,
                            Monto = new Dinero(aDevolver),
                            Saldo = new Dinero(aDevolver),
                            Estado = EstadoNotaCredito.Activa,
                            UsuarioId = usuarioId,
                            Fecha = _reloj.UtcAhora
                        };
                        nota.Folio = $"NC-{_reloj.UtcAhora:yyyyMMdd}-{nota.Id.ToString("N")[..6].ToUpperInvariant()}";
                        await _notasCredito.AgregarAsync(nota, ct);
                    }
                    else
                    {
                        await _movimientosCaja.AgregarAsync(new MovimientoCaja
                        {
                            SesionCajaId = sesion!.Id,
                            Tipo = TipoMovimientoCaja.Reembolso,
                            Monto = new Dinero(aDevolver),
                            Concepto = $"Diferencia de cambio, venta {origen.Folio}",
                            UsuarioId = usuarioId,
                            Fecha = _reloj.UtcAhora
                        }, ct);
                    }
                    await _uow.GuardarCambiosAsync(ct);
                }
            }, ct);
        }
        catch (CambioFallidoException ex)
        {
            return Result.Falla<ResultadoCambioDto>(ex.Message);
        }

        await _auditoria.RegistrarAsync("Cambio de prenda",
            $"Venta {origen.Folio}: regresa {preview.Credito:C2}, se lleva {preview.TotalNuevo:C2}" +
            (ventaNueva is not null ? $" (venta {ventaNueva.Folio})" : "") +
            (aDevolver > 0 ? $"; devuelto {aDevolver:C2}" : ""), "Venta", origen.Id, ct);

        // Ticket de la venta nueva, ya confirmada (si falla la impresora no se pierde nada).
        var avisos = new List<string>(ventaNueva?.Advertencias ?? Array.Empty<string>());
        if (ventaNueva is not null)
        {
            try
            {
                var v = await _ventas.ObtenerConDetalleAsync(ventaNueva.VentaId, ct);
                if (v is not null) await _impresora.ImprimirVentaAsync(v, ct);
            }
            catch { avisos.Add("No se pudo imprimir el ticket del cambio. Usa «Última venta» para reimprimir."); }
        }

        return Result.Ok(new ResultadoCambioDto(
            ventaNueva?.VentaId, ventaNueva?.Folio, preview.Credito, preview.TotalNuevo,
            preview.HayQueCobrar ? preview.Diferencia : 0m, aDevolver, ventaNueva?.Cambio ?? 0m, avisos));
    }

    private sealed class CambioFallidoException : Exception
    {
        public CambioFallidoException(string mensaje) : base(mensaje) { }
    }
}
