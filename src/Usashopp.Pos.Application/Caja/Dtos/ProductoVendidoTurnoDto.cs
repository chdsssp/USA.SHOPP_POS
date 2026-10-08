namespace Usashopp.Pos.Application.Caja.Dtos;

/// <summary>
/// Un producto vendido en el turno (sesión de caja) actual, agregado por descripción y precio.
/// <paramref name="PrecioFinal"/> es el precio unitario ya con descuentos (de línea y global).
/// </summary>
public record ProductoVendidoTurnoDto(
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal PrecioFinal,
    decimal Descuento)
{
    public bool TieneDescuento => Descuento >= 0.005m;
}
