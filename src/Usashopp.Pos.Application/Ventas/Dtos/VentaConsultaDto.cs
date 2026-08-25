namespace Usashopp.Pos.Application.Ventas.Dtos;

/// <summary>Fila del historial de ventas.</summary>
public record VentaResumenDto(
    Guid Id,
    string Folio,
    DateTime Fecha,
    decimal Total,
    int Articulos,
    string Estado);

public record VentaLineaDetalleDto(
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Importe,
    decimal Descuento = 0)
{
    /// <summary>Precio × cantidad, antes del descuento de línea.</summary>
    public decimal Bruto => PrecioUnitario * Cantidad;
    public bool TieneDescuento => Descuento > 0;
}

public record PagoResumenDto(string Metodo, decimal Monto);

/// <summary>Detalle completo de una venta (para ver/reimprimir).</summary>
public record VentaDetalleDto(
    Guid Id,
    string Folio,
    DateTime Fecha,
    decimal Subtotal,
    decimal Total,
    decimal Cambio,
    string Estado,
    IReadOnlyList<VentaLineaDetalleDto> Lineas,
    IReadOnlyList<PagoResumenDto> Pagos,
    string? Notas = null,
    decimal DescuentoGlobal = 0)
{
    public bool TieneDescuentoGlobal => DescuentoGlobal > 0;
}
