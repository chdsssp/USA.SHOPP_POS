using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Inventario;

/// <summary>Consulta y ajuste de existencias.</summary>
public class InventarioService
{
    private readonly IVarianteRepository _variantes;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public InventarioService(
        IVarianteRepository variantes,
        IMovimientoInventarioRepository movimientos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _variantes = variantes;
        _movimientos = movimientos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task<IReadOnlyList<VarianteInventarioDto>> ListarAsync(
        string? texto = null, bool soloBajoStock = false, CancellationToken ct = default)
    {
        var variantes = await _variantes.ListarInventarioAsync(texto, soloBajoStock, false, ct);
        return variantes.Select(Mapear).ToList();
    }

    /// <summary>Kardex de una variante: sus movimientos con el saldo acumulado (más reciente primero).</summary>
    public async Task<IReadOnlyList<MovimientoKardexDto>> ObtenerKardexAsync(Guid varianteId, CancellationToken ct = default)
    {
        var movimientos = await _movimientos.ListarPorVarianteAsync(varianteId, ct);

        var renglones = new List<MovimientoKardexDto>(movimientos.Count);
        var saldo = 0;
        foreach (var m in movimientos) // vienen en orden ascendente por fecha
        {
            saldo += m.Cantidad;
            renglones.Add(new MovimientoKardexDto(m.Fecha, DescribirTipo(m.Tipo), m.Cantidad, saldo, m.Motivo));
        }

        renglones.Reverse(); // mostrar el más reciente primero
        return renglones;
    }

    private static string DescribirTipo(TipoMovimientoInventario tipo) => tipo switch
    {
        TipoMovimientoInventario.InventarioInicial => "Inventario inicial",
        TipoMovimientoInventario.Venta => "Venta",
        TipoMovimientoInventario.Compra => "Compra",
        TipoMovimientoInventario.AjustePositivo => "Ajuste (+)",
        TipoMovimientoInventario.AjusteNegativo => "Ajuste (−)",
        TipoMovimientoInventario.Devolucion => "Devolución",
        TipoMovimientoInventario.Merma => "Merma",
        _ => tipo.ToString()
    };

    public async Task<Result> AjustarStockAsync(AjusteStockDto dto, CancellationToken ct = default)
    {
        if (dto.NuevaCantidad < 0)
            return Result.Falla("La cantidad no puede ser negativa.");

        var variante = await _variantes.ObtenerPorIdAsync(dto.VarianteId, ct);
        if (variante is null)
            return Result.Falla("La variante no existe.");

        var delta = dto.NuevaCantidad - variante.StockActual;
        if (delta == 0)
            return Result.Ok();

        var usuarioId = _usuario.UsuarioId ?? Guid.Empty;

        await _uow.EjecutarEnTransaccionAsync(async () =>
        {
            variante.AplicarCambioStock(delta);
            _variantes.Actualizar(variante);

            await _movimientos.AgregarAsync(new MovimientoInventario
            {
                VarianteId = variante.Id,
                Tipo = delta > 0 ? TipoMovimientoInventario.AjustePositivo : TipoMovimientoInventario.AjusteNegativo,
                Cantidad = delta,
                Motivo = dto.Motivo ?? "Ajuste manual de inventario",
                UsuarioId = usuarioId,
                Fecha = _reloj.UtcAhora
            }, ct);

            await _uow.GuardarCambiosAsync(ct);
        }, ct);

        return Result.Ok();
    }

    private static VarianteInventarioDto Mapear(VarianteProducto v) => new(
        v.Id,
        v.ProductoId,
        v.Producto?.Nombre ?? "Producto",
        v.Producto?.Categoria?.Nombre,
        v.Producto?.Marca,
        v.Talla,
        v.Color,
        v.Sku.Valor,
        v.CodigoBarras?.Valor,
        v.PrecioVenta.Monto,
        v.Costo.Monto,
        v.StockActual,
        v.StockMinimo,
        v.EstaBajoMinimo,
        v.Activo);
}
