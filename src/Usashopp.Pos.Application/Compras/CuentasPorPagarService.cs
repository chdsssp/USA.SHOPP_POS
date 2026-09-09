using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Compras;

/// <summary>Cuentas por pagar: estado de cuenta de una compra y registro de abonos.</summary>
public class CuentasPorPagarService
{
    private readonly ICompraRepository _compras;
    private readonly IRepository<PagoCompra> _pagos;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public CuentasPorPagarService(
        ICompraRepository compras,
        IRepository<PagoCompra> pagos,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _compras = compras;
        _pagos = pagos;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<EstadoCuentaCompraDto?> ObtenerEstadoCuentaAsync(Guid compraId, CancellationToken ct = default)
    {
        var compra = await _compras.ObtenerConDetalleAsync(compraId, ct);
        if (compra is null) return null;

        var pagos = await _pagos.ListarAsync(p => p.CompraId == compraId, ct);
        var total = compra.Total.Monto;
        var pagado = pagos.Sum(p => p.Monto.Monto);

        return new EstadoCuentaCompraDto(
            compra.Id, compra.Folio, compra.Proveedor?.Nombre ?? "—", total, pagado, total - pagado,
            pagos.OrderByDescending(p => p.Fecha)
                .Select(p => new PagoCompraDto(p.Fecha, p.Monto.Monto, p.Metodo.ToString(), p.Nota))
                .ToList());
    }

    public async Task<Result> RegistrarPagoAsync(RegistrarPagoCompraDto dto, CancellationToken ct = default)
    {
        if (dto.Monto <= 0)
            return Result.Falla("El monto del pago debe ser mayor que cero.");

        var compra = await _compras.ObtenerConDetalleAsync(dto.CompraId, ct);
        if (compra is null) return Result.Falla("La compra no existe.");

        var pagos = await _pagos.ListarAsync(p => p.CompraId == dto.CompraId, ct);
        var saldo = compra.Total.Monto - pagos.Sum(p => p.Monto.Monto);
        if (saldo <= 0) return Result.Falla("La compra ya está saldada.");
        if (dto.Monto > saldo) return Result.Falla($"El pago excede el saldo pendiente ({saldo:C2}).");

        await _pagos.AgregarAsync(new PagoCompra
        {
            CompraId = compra.Id,
            Monto = new Domain.ValueObjects.Dinero(dto.Monto),
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim(),
            UsuarioId = _usuario.UsuarioId ?? Guid.Empty,
            Fecha = _reloj.UtcAhora
        }, ct);
        await _uow.GuardarCambiosAsync(ct);

        await _auditoria.RegistrarAsync(
            "Pago a proveedor", $"Compra {compra.Folio}; abono {dto.Monto:C2}", "Compra", compra.Id);
        return Result.Ok();
    }
}
