using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Compras;

/// <summary>
/// Órdenes de compra: crear una orden (sin ingresar stock) y recibir mercancía (total o
/// parcial), que ingresa stock, actualiza costo y avanza el estado de la compra.
/// </summary>
public class OrdenCompraService
{
    private readonly ICompraRepository _compras;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly IRepository<Proveedor> _proveedores;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public OrdenCompraService(
        ICompraRepository compras,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        IConfiguracionTiendaRepository configuracion,
        IRepository<Proveedor> proveedores,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _compras = compras;
        _variantes = variantes;
        _movimientos = movimientos;
        _configuracion = configuracion;
        _proveedores = proveedores;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    /// <summary>Crea una orden de compra (estado Ordenada); no ingresa stock.</summary>
    public async Task<Result<Guid>> CrearOrdenAsync(NuevaCompraDto dto, CancellationToken ct = default)
    {
        if (dto.Lineas.Count == 0)
            return Result.Falla<Guid>("La orden debe tener al menos una línea.");
        if (dto.Lineas.Any(l => l.Cantidad <= 0))
            return Result.Falla<Guid>("Las cantidades deben ser mayores que cero.");

        var proveedor = await _proveedores.ObtenerPorIdAsync(dto.ProveedorId, ct);
        if (proveedor is null)
            return Result.Falla<Guid>("El proveedor no existe.");

        var config = await _configuracion.ObtenerAsync(ct);

        var compra = new Compra
        {
            ProveedorId = dto.ProveedorId,
            Fecha = _reloj.UtcAhora,
            Folio = $"{config.PrefijoFolioCompra}{config.ConsecutivoCompra:D6}"
        };
        foreach (var l in dto.Lineas)
        {
            if (await _variantes.ObtenerPorIdAsync(l.VarianteId, ct) is null)
                return Result.Falla<Guid>($"La variante {l.VarianteId} no existe.");
            compra.Detalles.Add(new DetalleCompra
            {
                VarianteId = l.VarianteId,
                Cantidad = l.Cantidad,
                CostoUnitario = new Dinero(l.CostoUnitario)
            });
        }
        compra.MarcarOrdenada();

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            await _compras.AgregarAsync(compra, ct);
            config.ConsecutivoCompra++;
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        await _auditoria.RegistrarAsync("Orden de compra creada", $"Folio {compra.Folio}", "Compra", compra.Id);
        return Result.Ok(compra.Id);
    }

    /// <summary>Recibe mercancía de una orden: ingresa stock de lo recibido y avanza el estado.</summary>
    public async Task<Result> RecibirAsync(RecepcionCompraDto dto, CancellationToken ct = default)
    {
        var compra = await _compras.ObtenerConDetalleAsync(dto.CompraId, ct);
        if (compra is null) return Result.Falla("La compra no existe.");
        if (!compra.PuedeRecibir) return Result.Falla("Esta compra no admite más recepciones.");

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;
        var recibidoTotal = 0;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            foreach (var linea in dto.Lineas)
            {
                if (linea.Cantidad <= 0) continue;
                var detalle = compra.Detalles.FirstOrDefault(d => d.Id == linea.DetalleId);
                if (detalle is null) continue;

                var cantidad = Math.Min(linea.Cantidad, detalle.Pendiente);
                if (cantidad <= 0) continue;

                var variante = await _variantes.ObtenerPorIdAsync(detalle.VarianteId, ct);
                if (variante is null) continue;

                variante.AplicarCambioStock(cantidad);
                variante.Costo = detalle.CostoUnitario; // costo al último de compra
                _variantes.Actualizar(variante);

                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.Compra,
                    Cantidad = cantidad,
                    Motivo = $"Recepción compra {compra.Folio}",
                    ReferenciaId = compra.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);

                detalle.CantidadRecibida += cantidad;
                recibidoTotal += cantidad;
            }

            compra.RecalcularRecepcion();
            _compras.Actualizar(compra);
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        if (recibidoTotal == 0)
            return Result.Falla("No se recibió ninguna cantidad (revisa lo pendiente).");

        await _auditoria.RegistrarAsync(
            "Recepción de compra", $"Folio {compra.Folio}; {recibidoTotal} pza(s); estado {compra.Estado}",
            "Compra", compra.Id);
        return Result.Ok();
    }
}
