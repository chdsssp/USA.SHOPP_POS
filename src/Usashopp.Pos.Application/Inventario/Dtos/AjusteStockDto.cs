namespace Usashopp.Pos.Application.Inventario.Dtos;

/// <summary>Ajuste manual de existencias: fija el stock a una nueva cantidad.</summary>
public record AjusteStockDto(Guid VarianteId, int NuevaCantidad, string? Motivo = null);
