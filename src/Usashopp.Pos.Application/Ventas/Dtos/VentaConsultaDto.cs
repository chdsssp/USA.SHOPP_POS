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
    decimal Importe);

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
    string? Notas = null);
