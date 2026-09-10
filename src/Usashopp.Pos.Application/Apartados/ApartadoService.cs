using Usashopp.Pos.Application.Apartados.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Apartados;

/// <summary>
/// Apartados (layaway): crear con anticipo (reservando stock), abonar, liquidar y cancelar
/// (devolviendo stock).
/// </summary>
public class ApartadoService
{
    private readonly IApartadoRepository _apartados;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly IRepository<Cliente> _clientes;
    private readonly ISesionCajaRepository _sesiones;
    private readonly IRepository<MovimientoCaja> _movimientosCaja;
    private readonly IVentaRepository _ventas;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public ApartadoService(
        IApartadoRepository apartados,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        IConfiguracionTiendaRepository configuracion,
        IRepository<Cliente> clientes,
        ISesionCajaRepository sesiones,
        IRepository<MovimientoCaja> movimientosCaja,
        IVentaRepository ventas,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _apartados = apartados;
        _variantes = variantes;
        _movimientos = movimientos;
        _configuracion = configuracion;
        _clientes = clientes;
        _sesiones = sesiones;
        _movimientosCaja = movimientosCaja;
        _ventas = ventas;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<IReadOnlyList<ApartadoResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _apartados.ListarAsync(ct);
        var ahora = _reloj.UtcAhora;
        return lista.Select(a => new ApartadoResumenDto(
            a.Id, a.Folio, a.Cliente?.Nombre ?? "—", a.Fecha,
            a.Total.Monto, a.TotalAbonado.Monto, a.Saldo.Monto, a.Estado.ToString(),
            a.FechaLimite, a.EstaVencido(ahora))).ToList();
    }

    public async Task<ApartadoDetalleDto?> ObtenerDetalleAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _apartados.ObtenerConDetalleAsync(id, ct);
        if (a is null) return null;
        return new ApartadoDetalleDto(
            a.Id, a.Folio, a.Cliente?.Nombre ?? "—", a.Fecha,
            a.Total.Monto, a.TotalAbonado.Monto, a.Saldo.Monto, a.Estado.ToString(),
            a.Detalles.Select(d => new LineaApartadoDetalleDto(d.Descripcion, d.Cantidad, d.PrecioUnitario.Monto, d.Importe.Monto)).ToList(),
            a.Abonos.OrderBy(ab => ab.Fecha).Select(ab => new AbonoDetalleDto(ab.Fecha, ab.Monto.Monto, ab.Metodo.ToString())).ToList());
    }

    public async Task<Result<Guid>> CrearAsync(NuevoApartadoDto dto, CancellationToken ct = default)
    {
        if (dto.Lineas.Count == 0) return Result.Falla<Guid>("El apartado debe tener al menos una línea.");
        if (dto.Lineas.Any(l => l.Cantidad <= 0)) return Result.Falla<Guid>("Las cantidades deben ser mayores que cero.");

        var cliente = await _clientes.ObtenerPorIdAsync(dto.ClienteId, ct);
        if (cliente is null) return Result.Falla<Guid>("Selecciona un cliente válido.");

        var config = await _configuracion.ObtenerAsync(ct);
        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        // Un anticipo en efectivo entra a la caja: exige caja abierta.
        SesionCaja? sesion = null;
        if (dto.AnticipoInicial > 0 && dto.MetodoAnticipo == MetodoPago.Efectivo)
        {
            sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
            if (sesion is null)
                return Result.Falla<Guid>("No hay caja abierta; ábrela para recibir el anticipo en efectivo.");
        }

        var apartado = new Apartado
        {
            ClienteId = dto.ClienteId,
            Fecha = _reloj.UtcAhora,
            FechaLimite = dto.FechaLimite,
            Folio = $"{config.PrefijoFolioApartado}{config.ConsecutivoApartado:D6}"
        };

        var afectadas = new List<(VarianteProducto v, int cant)>();
        foreach (var l in dto.Lineas)
        {
            var variante = await _variantes.ObtenerPorIdAsync(l.VarianteId, ct);
            if (variante is null) return Result.Falla<Guid>($"La variante {l.VarianteId} no existe.");
            if (!config.PermitirVentaStockNegativo && variante.StockActual < l.Cantidad)
                return Result.Falla<Guid>($"Stock insuficiente de «{variante.DescripcionCompleta}» para apartar.");

            apartado.Detalles.Add(new DetalleApartado
            {
                VarianteId = variante.Id,
                Descripcion = variante.DescripcionCompleta,
                Cantidad = l.Cantidad,
                PrecioUnitario = new Dinero(l.PrecioUnitario)
            });
            afectadas.Add((variante, l.Cantidad));
        }

        if (dto.AnticipoInicial > 0)
            apartado.Abonos.Add(new AbonoApartado
            {
                Monto = new Dinero(dto.AnticipoInicial),
                Metodo = dto.MetodoAnticipo,
                Fecha = _reloj.UtcAhora,
                UsuarioId = usuarioId
            });

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            await _apartados.AgregarAsync(apartado, ct);
            foreach (var (v, cant) in afectadas)
            {
                v.AplicarCambioStock(-cant); // se reserva (sale del stock disponible)
                _variantes.Actualizar(v);
                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = v.Id,
                    Tipo = TipoMovimientoInventario.AjusteNegativo,
                    Cantidad = -cant,
                    Motivo = $"Apartado {apartado.Folio}",
                    ReferenciaId = apartado.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }
            if (sesion is not null)
                await RegistrarIngresoCajaAsync(sesion.Id, dto.AnticipoInicial, $"Anticipo apartado {apartado.Folio}", usuarioId, ct);

            config.ConsecutivoApartado++;
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok(apartado.Id);
    }

    private async Task RegistrarIngresoCajaAsync(Guid sesionId, decimal monto, string concepto, Guid usuarioId, CancellationToken ct) =>
        await _movimientosCaja.AgregarAsync(new MovimientoCaja
        {
            SesionCajaId = sesionId,
            Tipo = TipoMovimientoCaja.Ingreso,
            Monto = new Dinero(monto),
            Concepto = concepto,
            UsuarioId = usuarioId,
            Fecha = _reloj.UtcAhora
        }, ct);

    public async Task<Result> AbonarAsync(NuevoAbonoDto dto, CancellationToken ct = default)
    {
        if (dto.Monto <= 0) return Result.Falla("El abono debe ser mayor que cero.");

        var apartado = await _apartados.ObtenerConDetalleAsync(dto.ApartadoId, ct);
        if (apartado is null) return Result.Falla("El apartado no existe.");
        if (apartado.Estado != EstadoApartado.Activo) return Result.Falla("El apartado no está activo.");
        if (dto.Monto > apartado.Saldo.Monto) return Result.Falla($"El abono excede el saldo ({apartado.Saldo}).");

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        SesionCaja? sesion = null;
        if (dto.Metodo == MetodoPago.Efectivo)
        {
            sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
            if (sesion is null)
                return Result.Falla("No hay caja abierta; ábrela para recibir el abono en efectivo.");
        }

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            apartado.Abonos.Add(new AbonoApartado
            {
                ApartadoId = apartado.Id,
                Monto = new Dinero(dto.Monto),
                Metodo = dto.Metodo,
                Fecha = _reloj.UtcAhora,
                UsuarioId = usuarioId
            });
            _apartados.Actualizar(apartado);

            if (sesion is not null)
                await RegistrarIngresoCajaAsync(sesion.Id, dto.Monto, $"Abono apartado {apartado.Folio}", usuarioId, ct);

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        await _auditoria.RegistrarAsync("Abono a apartado", $"Apartado {apartado.Folio}; {dto.Monto:C2}", "Apartado", apartado.Id);
        return Result.Ok();
    }

    public async Task<Result> LiquidarAsync(Guid id, CancellationToken ct = default)
    {
        var apartado = await _apartados.ObtenerConDetalleAsync(id, ct);
        if (apartado is null) return Result.Falla("El apartado no existe.");
        if (apartado.Estado != EstadoApartado.Activo) return Result.Falla("El apartado no está activo.");
        if (apartado.Saldo.Monto > 0) return Result.Falla($"No se puede liquidar: saldo pendiente de {apartado.Saldo}.");

        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        if (sesion is null) return Result.Falla("No hay caja abierta; ábrela para liquidar el apartado.");

        var config = await _configuracion.ObtenerAsync(ct);
        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        // Venta que reconoce el ingreso del apartado. Se paga con método "Otro" (el efectivo ya
        // entró a caja vía los abonos); el stock ya salió al crear el apartado, no se re-descuenta.
        var venta = new Venta
        {
            SesionCajaId = sesion.Id,
            UsuarioId = usuarioId,
            ClienteId = apartado.ClienteId,
            Fecha = _reloj.UtcAhora,
            Notas = $"Liquidación apartado {apartado.Folio}"
        };
        foreach (var d in apartado.Detalles)
            venta.AgregarLinea(new DetalleVenta
            {
                VarianteId = d.VarianteId,
                Descripcion = d.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario
            });
        venta.RegistrarPago(new Pago
        {
            Metodo = MetodoPago.Otro,
            Monto = apartado.Total,
            Referencia = $"Apartado {apartado.Folio}",
            Fecha = _reloj.UtcAhora
        });

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            venta.Folio = $"{config.PrefijoFolioVenta}{config.ConsecutivoVenta:D6}";
            venta.MarcarPagada();
            await _ventas.AgregarAsync(venta, ct);
            config.ConsecutivoVenta++;

            apartado.Liquidar();
            _apartados.Actualizar(apartado);
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        await _auditoria.RegistrarAsync(
            "Liquidación de apartado", $"Apartado {apartado.Folio} → venta {venta.Folio}", "Apartado", apartado.Id);
        return Result.Ok();
    }

    public async Task<Result> CancelarAsync(Guid id, CancellationToken ct = default)
    {
        var apartado = await _apartados.ObtenerConDetalleAsync(id, ct);
        if (apartado is null) return Result.Falla("El apartado no existe.");
        if (apartado.Estado != EstadoApartado.Activo) return Result.Falla("El apartado no está activo.");

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            foreach (var d in apartado.Detalles)
            {
                var variante = await _variantes.ObtenerPorIdAsync(d.VarianteId, ct);
                if (variante is null) continue;
                variante.AplicarCambioStock(d.Cantidad); // devuelve el stock reservado
                _variantes.Actualizar(variante);
                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.AjustePositivo,
                    Cantidad = d.Cantidad,
                    Motivo = $"Cancelación apartado {apartado.Folio}",
                    ReferenciaId = apartado.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }
            apartado.Cancelar();
            _apartados.Actualizar(apartado);
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok();
    }
}
