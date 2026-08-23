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
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public ApartadoService(
        IApartadoRepository apartados,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        IConfiguracionTiendaRepository configuracion,
        IRepository<Cliente> clientes,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _apartados = apartados;
        _variantes = variantes;
        _movimientos = movimientos;
        _configuracion = configuracion;
        _clientes = clientes;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ApartadoResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _apartados.ListarAsync(ct);
        return lista.Select(a => new ApartadoResumenDto(
            a.Id, a.Folio, a.Cliente?.Nombre ?? "—", a.Fecha,
            a.Total.Monto, a.TotalAbonado.Monto, a.Saldo.Monto, a.Estado.ToString())).ToList();
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

        var apartado = new Apartado
        {
            ClienteId = dto.ClienteId,
            Fecha = _reloj.UtcAhora,
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
            config.ConsecutivoApartado++;
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok(apartado.Id);
    }

    public async Task<Result> AbonarAsync(NuevoAbonoDto dto, CancellationToken ct = default)
    {
        if (dto.Monto <= 0) return Result.Falla("El abono debe ser mayor que cero.");

        var apartado = await _apartados.ObtenerConDetalleAsync(dto.ApartadoId, ct);
        if (apartado is null) return Result.Falla("El apartado no existe.");
        if (apartado.Estado != EstadoApartado.Activo) return Result.Falla("El apartado no está activo.");
        if (dto.Monto > apartado.Saldo.Monto) return Result.Falla($"El abono excede el saldo ({apartado.Saldo}).");

        apartado.Abonos.Add(new AbonoApartado
        {
            ApartadoId = apartado.Id,
            Monto = new Dinero(dto.Monto),
            Metodo = dto.Metodo,
            Fecha = _reloj.UtcAhora,
            UsuarioId = _usuario.UsuarioId ?? Guid.Empty
        });
        _apartados.Actualizar(apartado);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> LiquidarAsync(Guid id, CancellationToken ct = default)
    {
        var apartado = await _apartados.ObtenerConDetalleAsync(id, ct);
        if (apartado is null) return Result.Falla("El apartado no existe.");
        if (apartado.Estado != EstadoApartado.Activo) return Result.Falla("El apartado no está activo.");
        if (apartado.Saldo.Monto > 0) return Result.Falla($"No se puede liquidar: saldo pendiente de {apartado.Saldo}.");

        apartado.Liquidar();
        _apartados.Actualizar(apartado);
        await _uow.GuardarCambiosAsync(ct);
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
