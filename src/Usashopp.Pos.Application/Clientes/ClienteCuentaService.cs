using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Clientes;

/// <summary>Actividad del cliente: historial de compras y cuenta de crédito (CxC).</summary>
public class ClienteCuentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<AbonoCliente> _abonos;
    private readonly NotaCreditoService _notas;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public ClienteCuentaService(
        IVentaRepository ventas,
        IRepository<Cliente> clientes,
        IRepository<AbonoCliente> abonos,
        NotaCreditoService notas,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _ventas = ventas;
        _clientes = clientes;
        _abonos = abonos;
        _notas = notas;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
        _auditoria = auditoria;
    }

    /// <summary>Historial de compras del cliente (de la más reciente a la más antigua).</summary>
    public async Task<IReadOnlyList<VentaResumenDto>> ListarComprasAsync(Guid clienteId, CancellationToken ct = default)
    {
        var ventas = await _ventas.ListarPorClienteAsync(clienteId, ct);
        return ventas
            .Select(v => new VentaResumenDto(
                v.Id, v.Folio, v.Fecha, v.Total.Monto, v.Detalles.Sum(d => d.Cantidad), v.Estado.ToString()))
            .ToList();
    }

    /// <summary>Cargos a crédito del cliente (suma de pagos con método Crédito en sus ventas).</summary>
    public async Task<decimal> CargosCreditoAsync(Guid clienteId, CancellationToken ct = default)
    {
        var ventas = await _ventas.ListarPorClienteAsync(clienteId, ct);
        return ventas
            .SelectMany(v => v.Pagos)
            .Where(p => p.Metodo == MetodoPago.Credito)
            .Sum(p => p.Monto.Monto);
    }

    public async Task<EstadoCreditoClienteDto?> ObtenerEstadoCreditoAsync(Guid clienteId, CancellationToken ct = default)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(clienteId, ct);
        if (cliente is null) return null;

        var cargos = await CargosCreditoAsync(clienteId, ct);
        var abonos = await _abonos.ListarAsync(a => a.ClienteId == clienteId, ct);
        var totalAbonos = abonos.Sum(a => a.Monto.Monto);
        var saldo = cargos - totalAbonos;
        var saldoNotas = await _notas.SaldoDisponibleAsync(clienteId, ct);

        return new EstadoCreditoClienteDto(
            cliente.Id, cliente.Nombre, cliente.LimiteCredito.Monto,
            cargos, totalAbonos, saldo, cliente.LimiteCredito.Monto - saldo, saldoNotas,
            abonos.OrderByDescending(a => a.Fecha)
                .Select(a => new AbonoClienteDto(a.Fecha, a.Monto.Monto, a.Metodo.ToString(), a.Nota))
                .ToList());
    }

    public async Task<Result> RegistrarAbonoAsync(RegistrarAbonoClienteDto dto, CancellationToken ct = default)
    {
        if (dto.Monto <= 0)
            return Result.Falla("El abono debe ser mayor que cero.");

        var cargos = await CargosCreditoAsync(dto.ClienteId, ct);
        var abonos = await _abonos.ListarAsync(a => a.ClienteId == dto.ClienteId, ct);
        var saldo = cargos - abonos.Sum(a => a.Monto.Monto);
        if (saldo <= 0) return Result.Falla("El cliente no tiene saldo de crédito por pagar.");
        if (dto.Monto > saldo) return Result.Falla($"El abono excede el saldo del cliente ({saldo:C2}).");

        await _abonos.AgregarAsync(new AbonoCliente
        {
            ClienteId = dto.ClienteId,
            Monto = new Domain.ValueObjects.Dinero(dto.Monto),
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim(),
            UsuarioId = _usuario.UsuarioId ?? Guid.Empty,
            Fecha = _reloj.UtcAhora
        }, ct);
        await _uow.GuardarCambiosAsync(ct);

        await _auditoria.RegistrarAsync("Abono de cliente", $"Abono {dto.Monto:C2}", "Cliente", dto.ClienteId);
        return Result.Ok();
    }
}
