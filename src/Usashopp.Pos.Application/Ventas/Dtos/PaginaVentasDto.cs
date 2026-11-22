namespace Usashopp.Pos.Application.Ventas.Dtos;

/// <summary>Página del historial de ventas: las filas y cuántas hay en total en el rango.</summary>
public record PaginaVentasDto(IReadOnlyList<VentaResumenDto> Items, int Total)
{
    public bool HayMas => Items.Count < Total;
}
