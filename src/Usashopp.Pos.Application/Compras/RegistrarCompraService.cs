using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Compras;

/// <summary>
/// Registra una compra a proveedor: reingresa stock y actualiza el costo de las variantes,
/// de forma transaccional.
/// </summary>
public class RegistrarCompraService
{
    private readonly ICompraRepository _compras;
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly IRepository<Proveedor> _proveedores;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public RegistrarCompraService(
        ICompraRepository compras,
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        IConfiguracionTiendaRepository configuracion,
        IRepository<Proveedor> proveedores,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _compras = compras;
        _variantes = variantes;
        _movimientos = movimientos;
        _configuracion = configuracion;
        _proveedores = proveedores;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task<Result<Guid>> EjecutarAsync(NuevaCompraDto dto, CancellationToken ct = default)
    {
        if (dto.Lineas.Count == 0)
            return Result.Falla<Guid>("La compra debe tener al menos una línea.");
        if (dto.Lineas.Any(l => l.Cantidad <= 0))
            return Result.Falla<Guid>("Las cantidades deben ser mayores que cero.");

        var proveedor = await _proveedores.ObtenerPorIdAsync(dto.ProveedorId, ct);
        if (proveedor is null)
            return Result.Falla<Guid>("El proveedor no existe.");

        var config = await _configuracion.ObtenerAsync(ct);
        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        var compra = new Compra
        {
            ProveedorId = dto.ProveedorId,
            Fecha = _reloj.UtcAhora,
            Folio = $"{config.PrefijoFolioCompra}{config.ConsecutivoCompra:D6}"
        };

        var afectadas = new List<(VarianteProducto variante, NuevaLineaCompraDto linea)>();
        foreach (var l in dto.Lineas)
        {
            var variante = await _variantes.ObtenerPorIdAsync(l.VarianteId, ct);
            if (variante is null)
                return Result.Falla<Guid>($"La variante {l.VarianteId} no existe.");

            compra.Detalles.Add(new DetalleCompra
            {
                VarianteId = variante.Id,
                Cantidad = l.Cantidad,
                CostoUnitario = new Dinero(l.CostoUnitario)
            });
            afectadas.Add((variante, l));
        }

        compra.MarcarRecibida();

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            await _compras.AgregarAsync(compra, ct);

            foreach (var (variante, l) in afectadas)
            {
                variante.AplicarCambioStock(l.Cantidad);
                variante.Costo = new Dinero(l.CostoUnitario); // actualiza el costo al último de compra
                _variantes.Actualizar(variante);

                await _movimientos.AgregarAsync(new MovimientoInventario
                {
                    VarianteId = variante.Id,
                    Tipo = TipoMovimientoInventario.Compra,
                    Cantidad = l.Cantidad,
                    ReferenciaId = compra.Id,
                    UsuarioId = usuarioId,
                    Fecha = _reloj.UtcAhora
                }, ct);
            }

            config.ConsecutivoCompra++;
            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok(compra.Id);
    }
}
