namespace Usashopp.Pos.Application.Catalogo.Dtos;

/// <summary>Un cambio de precio de venta de una variante.</summary>
public record HistorialPrecioDto(DateTime Fecha, decimal PrecioAnterior, decimal PrecioNuevo)
{
    /// <summary>Variación respecto al precio anterior.</summary>
    public decimal Diferencia => PrecioNuevo - PrecioAnterior;
}
