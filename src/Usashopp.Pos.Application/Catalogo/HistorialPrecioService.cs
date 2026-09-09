using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Catalogo;

/// <summary>Consulta del historial de precios de venta de una variante.</summary>
public class HistorialPrecioService
{
    private readonly IRepository<HistorialPrecio> _historial;

    public HistorialPrecioService(IRepository<HistorialPrecio> historial) => _historial = historial;

    public async Task<IReadOnlyList<HistorialPrecioDto>> ListarPorVarianteAsync(Guid varianteId, CancellationToken ct = default)
    {
        var lista = await _historial.ListarAsync(h => h.VarianteId == varianteId, ct);
        return lista
            .OrderByDescending(h => h.Fecha)
            .Select(h => new HistorialPrecioDto(h.Fecha, h.PrecioAnterior.Monto, h.PrecioNuevo.Monto))
            .ToList();
    }
}
