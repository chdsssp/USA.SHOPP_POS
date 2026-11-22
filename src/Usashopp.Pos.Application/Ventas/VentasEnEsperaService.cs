using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Línea de un carrito suspendido.</summary>
public record LineaEsperaDto(
    Guid VarianteId,
    string Descripcion,
    string Sku,
    decimal PrecioUnitario,
    decimal PrecioVenta,
    int Cantidad,
    TipoDescuento? DescuentoTipo = null,
    decimal DescuentoValor = 0,
    int StockDisponible = 0);

/// <summary>Carrito a suspender (o borrador a guardar).</summary>
public record NuevaVentaEsperaDto(
    string? Etiqueta,
    Guid? ClienteId,
    TipoDescuento? DescuentoGlobalTipo,
    decimal DescuentoGlobalValor,
    string? Notas,
    IReadOnlyList<LineaEsperaDto> Lineas);

/// <summary>Venta en espera tal como se lista o se recupera.</summary>
public record VentaEsperaDto(
    Guid Id,
    DateTime Fecha,
    string Etiqueta,
    Guid? ClienteId,
    string? ClienteNombre,
    string? Usuario,
    TipoDescuento? DescuentoGlobalTipo,
    decimal DescuentoGlobalValor,
    string? Notas,
    IReadOnlyList<LineaEsperaDto> Lineas,
    decimal Total)
{
    public int Articulos => Lineas.Sum(l => l.Cantidad);
    public string HoraTexto => Fecha.ToString("HH:mm");
}

/// <summary>Venta recuperada con avisos de precio/existencia cambiados desde que se suspendió.</summary>
public record VentaRecuperadaDto(VentaEsperaDto Venta, IReadOnlyList<string> Avisos);

/// <summary>
/// Ventas en espera y borrador del carrito, guardados en la base: sobreviven a cierres
/// inesperados, cortes de luz y reinicios (antes vivían solo en memoria).
/// </summary>
public class VentasEnEsperaService
{
    private readonly IVentaSuspendidaRepository _repo;
    private readonly IVarianteRepository _variantes;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<Usuario> _usuarios;
    private readonly ISesionCajaRepository _sesiones;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public VentasEnEsperaService(IVentaSuspendidaRepository repo, IVarianteRepository variantes,
        IRepository<Cliente> clientes, IRepository<Usuario> usuarios, ISesionCajaRepository sesiones,
        ICurrentUser usuario, IDateTime reloj, IUnitOfWork uow)
    {
        _repo = repo;
        _variantes = variantes;
        _clientes = clientes;
        _usuarios = usuarios;
        _sesiones = sesiones;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    // ---------------- Ventas en espera ----------------

    public async Task<Result<Guid>> SuspenderAsync(NuevaVentaEsperaDto dto, CancellationToken ct = default)
    {
        if (dto.Lineas.Count == 0) return Result.Falla<Guid>("El carrito está vacío.");
        if (_usuario.UsuarioId is not { } usuarioId) return Result.Falla<Guid>("No hay un usuario autenticado.");

        var entidad = await CrearEntidadAsync(dto, usuarioId, esBorrador: false, ct);
        await _repo.AgregarAsync(entidad, ct);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok(entidad.Id);
    }

    public async Task<IReadOnlyList<VentaEsperaDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _repo.ListarConLineasAsync(esBorrador: false, usuarioId: null, ct);
        var resultado = new List<VentaEsperaDto>();
        foreach (var v in lista) resultado.Add(await MapearAsync(v, ct));
        return resultado;
    }

    public Task<int> ContarAsync(CancellationToken ct = default) => _repo.ContarAsync(esBorrador: false, ct);

    /// <summary>Devuelve la venta (revalidando precio y existencia actuales) y la quita de la espera.</summary>
    public async Task<Result<VentaRecuperadaDto>> RecuperarAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _repo.ObtenerConLineasAsync(id, ct);
        if (v is null) return Result.Falla<VentaRecuperadaDto>("La venta en espera ya no existe (¿la recuperó otra caja?).");

        var (dto, avisos) = await RevalidarAsync(v, ct);
        _repo.Eliminar(v);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok(new VentaRecuperadaDto(dto, avisos));
    }

    public async Task<Result> DescartarAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _repo.ObtenerConLineasAsync(id, ct);
        if (v is null) return Result.Ok();
        _repo.Eliminar(v);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    // ---------------- Borrador del carrito activo ----------------

    /// <summary>Reemplaza el borrador del usuario actual (uno por usuario). Vacío = lo elimina.</summary>
    public async Task GuardarBorradorAsync(NuevaVentaEsperaDto dto, CancellationToken ct = default)
    {
        if (_usuario.UsuarioId is not { } usuarioId) return;
        foreach (var previo in await _repo.ListarConLineasAsync(esBorrador: true, usuarioId, ct))
            _repo.Eliminar(previo);
        if (dto.Lineas.Count > 0)
            await _repo.AgregarAsync(await CrearEntidadAsync(dto, usuarioId, esBorrador: true, ct), ct);
        await _uow.GuardarCambiosAsync(ct);
    }

    /// <summary>Borrador pendiente del usuario actual (p. ej. tras un cierre inesperado), revalidado.</summary>
    public async Task<VentaRecuperadaDto?> ObtenerBorradorAsync(CancellationToken ct = default)
    {
        if (_usuario.UsuarioId is not { } usuarioId) return null;
        var v = (await _repo.ListarConLineasAsync(esBorrador: true, usuarioId, ct)).FirstOrDefault();
        if (v is null || v.Lineas.Count == 0) return null;
        var (dto, avisos) = await RevalidarAsync(v, ct);
        return new VentaRecuperadaDto(dto, avisos);
    }

    public Task EliminarBorradorAsync(CancellationToken ct = default) =>
        GuardarBorradorAsync(new NuevaVentaEsperaDto(null, null, null, 0, null, Array.Empty<LineaEsperaDto>()), ct);

    // ---------------- Internos ----------------

    private async Task<VentaSuspendida> CrearEntidadAsync(NuevaVentaEsperaDto dto, Guid usuarioId, bool esBorrador, CancellationToken ct)
    {
        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        var etiqueta = dto.Etiqueta;
        if (string.IsNullOrWhiteSpace(etiqueta))
            etiqueta = $"Ticket {Fechas.UtcALocal(_reloj.UtcAhora):HH:mm}";

        var entidad = new VentaSuspendida
        {
            UsuarioId = usuarioId,
            SesionCajaId = sesion?.Id,
            Etiqueta = etiqueta.Trim(),
            ClienteId = dto.ClienteId,
            DescuentoGlobal = CalculadoraTotales.CrearDescuento(dto.DescuentoGlobalTipo, dto.DescuentoGlobalValor),
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            Fecha = _reloj.UtcAhora,
            EsBorrador = esBorrador
        };
        var orden = 0;
        foreach (var l in dto.Lineas)
            entidad.Lineas.Add(new LineaVentaSuspendida
            {
                VarianteId = l.VarianteId,
                Descripcion = l.Descripcion,
                Sku = l.Sku,
                PrecioUnitario = new Dinero(l.PrecioUnitario),
                PrecioVenta = new Dinero(l.PrecioVenta),
                Cantidad = Math.Max(1, l.Cantidad),
                Descuento = CalculadoraTotales.CrearDescuento(l.DescuentoTipo, l.DescuentoValor),
                Orden = orden++
            });
        return entidad;
    }

    private async Task<VentaEsperaDto> MapearAsync(VentaSuspendida v, CancellationToken ct, IReadOnlyList<LineaEsperaDto>? lineas = null)
    {
        lineas ??= v.Lineas.OrderBy(l => l.Orden).Select(l => new LineaEsperaDto(
            l.VarianteId, l.Descripcion, l.Sku, l.PrecioUnitario.Monto, l.PrecioVenta.Monto, l.Cantidad,
            l.Descuento?.Tipo, l.Descuento?.Valor ?? 0)).ToList();

        var total = CalculadoraTotales.Calcular(
            lineas.Select(l => new LineaCalculo(l.VarianteId, l.Cantidad, l.PrecioVenta, l.DescuentoTipo, l.DescuentoValor)).ToList(),
            v.DescuentoGlobal?.Tipo, v.DescuentoGlobal?.Valor ?? 0).Total;

        var cliente = v.ClienteId is { } cid ? (await _clientes.ObtenerPorIdAsync(cid, ct))?.Nombre : null;
        var usuario = (await _usuarios.ObtenerPorIdAsync(v.UsuarioId, ct))?.Nombre;

        return new VentaEsperaDto(v.Id, Fechas.UtcALocal(v.Fecha), v.Etiqueta, v.ClienteId, cliente, usuario,
            v.DescuentoGlobal?.Tipo, v.DescuentoGlobal?.Valor ?? 0, v.Notas, lineas, total);
    }

    /// <summary>Precio y existencia ACTUALES de cada línea; avisa lo que cambió desde que se suspendió.</summary>
    private async Task<(VentaEsperaDto, IReadOnlyList<string>)> RevalidarAsync(VentaSuspendida v, CancellationToken ct)
    {
        var avisos = new List<string>();
        var lineas = new List<LineaEsperaDto>();
        foreach (var l in v.Lineas.OrderBy(x => x.Orden))
        {
            var variante = await _variantes.ObtenerPorIdAsync(l.VarianteId, ct);
            if (variante is null || !variante.Activo)
            {
                avisos.Add($"«{l.Descripcion}» ya no está disponible y se quitó.");
                continue;
            }

            var precioActual = variante.PrecioVenta.Monto;
            var precioVenta = l.PrecioVenta.Monto;
            if (precioActual != l.PrecioUnitario.Monto)
            {
                avisos.Add($"«{l.Descripcion}» cambió de precio: {l.PrecioUnitario.Monto:C2} → {precioActual:C2}.");
                // Si no se había editado a mano, se toma el precio nuevo.
                if (l.PrecioVenta.Monto == l.PrecioUnitario.Monto) precioVenta = precioActual;
            }
            if (variante.StockActual < l.Cantidad)
                avisos.Add($"«{l.Descripcion}»: solo hay {variante.StockActual} en existencia (se pidieron {l.Cantidad}).");

            lineas.Add(new LineaEsperaDto(l.VarianteId, variante.DescripcionCompleta, variante.Sku.Valor,
                precioActual, precioVenta, l.Cantidad, l.Descuento?.Tipo, l.Descuento?.Valor ?? 0, variante.StockActual));
        }
        return (await MapearAsync(v, ct, lineas), avisos);
    }
}
