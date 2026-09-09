using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Compras;

/// <summary>
/// Devolución de mercancía recibida a un proveedor: baja stock con un movimiento
/// <see cref="TipoMovimientoInventario.DevolucionProveedor"/>. No permite devolver más de lo
/// recibido (menos lo ya devuelto) ni más de lo que hay en existencia.
/// </summary>
public class DevolucionProveedorService
{
    private readonly ICompraRepository _compras;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public DevolucionProveedorService(
        ICompraRepository compras,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _compras = compras;
        _variantes = variantes;
        _movimientos = movimientos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<Result> EjecutarAsync(DevolucionProveedorDto dto, CancellationToken ct = default)
    {
        var compra = await _compras.ObtenerConDetalleAsync(dto.CompraId, ct);
        if (compra is null) return Result.Falla("La compra no existe.");

        var recibidoPorVariante = compra.Detalles
            .GroupBy(d => d.VarianteId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.CantidadRecibida));

        var movimientos = await _movimientos.ListarPorReferenciaAsync(compra.Id, ct);
        var devueltoPorVariante = movimientos
            .Where(m => m.Tipo == TipoMovimientoInventario.DevolucionProveedor)
            .GroupBy(m => m.VarianteId)
            .ToDictionary(g => g.Key, g => g.Sum(m => Math.Abs(m.Cantidad)));

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;
        var devueltoTotal = 0;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            foreach (var linea in dto.Lineas)
            {
                if (linea.Cantidad <= 0) continue;
                if (!recibidoPorVariante.TryGetValue(linea.VarianteId, out var recibido)) continue;

                var yaDevuelto = devueltoPorVariante.GetValueOrDefault(linea.VarianteId, 0);
                var variante = await _variantes.ObtenerPorIdAsync(linea.VarianteId, ct);
                if (variante is null) continue;

                // Tope: lo que resta por devolver de lo recibido y lo que hay físicamente en stock.
                var tope = Math.Min(recibido - yaDevuelto, variante.StockActual);
                var cantidad = Math.Min(linea.Cantidad, tope);
                if (cantidad <= 0) continue;

                variante.AplicarCambioStock(-cantidad);
                _variantes.Actualizar(variante);

                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.DevolucionProveedor,
                    Cantidad = -cantidad,
                    Motivo = $"Devolución a proveedor · compra {compra.Folio}",
                    ReferenciaId = compra.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);

                devueltoTotal += cantidad;
            }

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        if (devueltoTotal == 0)
            return Result.Falla("No se devolvió ninguna cantidad (revisa lo recibido y el stock).");

        await _auditoria.RegistrarAsync(
            "Devolución a proveedor", $"Compra {compra.Folio}; {devueltoTotal} pza(s)", "Compra", compra.Id);
        return Result.Ok();
    }
}
