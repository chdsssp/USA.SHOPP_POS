namespace Usashopp.Pos.Domain.Enums;

public enum TipoMovimientoInventario
{
    InventarioInicial = 0,
    Venta = 1,
    Compra = 2,
    AjustePositivo = 3,
    AjusteNegativo = 4,
    Devolucion = 5,
    Merma = 6,
    /// <summary>Devolución de mercancía a un proveedor (baja stock).</summary>
    DevolucionProveedor = 7
}
