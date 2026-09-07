using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Consulta de notas de crédito (saldo a favor) de los clientes.</summary>
public class NotaCreditoService
{
    private readonly IRepository<NotaCredito> _notas;

    public NotaCreditoService(IRepository<NotaCredito> notas) => _notas = notas;

    /// <summary>Notas de crédito de un cliente, de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<NotaCreditoDto>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct = default)
    {
        var notas = await _notas.ListarAsync(n => n.ClienteId == clienteId, ct);
        return notas
            .OrderByDescending(n => n.Fecha)
            .Select(n => new NotaCreditoDto(n.Id, n.Folio, n.Fecha, n.Monto.Monto, n.Saldo.Monto, n.Estado.ToString()))
            .ToList();
    }

    /// <summary>Saldo total disponible (notas activas) de un cliente.</summary>
    public async Task<decimal> SaldoDisponibleAsync(Guid clienteId, CancellationToken ct = default)
    {
        var notas = await _notas.ListarAsync(
            n => n.ClienteId == clienteId && n.Estado == EstadoNotaCredito.Activa, ct);
        return notas.Sum(n => n.Saldo.Monto);
    }
}
