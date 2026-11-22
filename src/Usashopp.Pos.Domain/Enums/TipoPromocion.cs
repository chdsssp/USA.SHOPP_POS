namespace Usashopp.Pos.Domain.Enums;

public enum TipoPromocion
{
    /// <summary>Rebaja en porcentaje (temporada, liquidación).</summary>
    Porcentaje = 0,
    /// <summary>Rebaja de un monto fijo por pieza.</summary>
    MontoPorPieza = 1,
    /// <summary>N×M: llevas N y pagas M (2×1, 3×2); se regalan las más baratas.</summary>
    NxM = 2,
    /// <summary>N por $X: paquete de N piezas a precio fijo (3 × $299).</summary>
    NPorPrecio = 3,
    /// <summary>Precio especial fijo por pieza durante la vigencia.</summary>
    PrecioEspecial = 4
}

public enum AlcancePromocion
{
    Todo = 0,
    Categoria = 1,
    Producto = 2,
    Variante = 3
}
