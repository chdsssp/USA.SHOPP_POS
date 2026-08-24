namespace Usashopp.Pos.Application.Ventas.Dtos;

/// <summary>Línea devolvible de una venta: lo vendido, lo ya devuelto y lo disponible.</summary>
public record DevolucionLineaDto(
    Guid VarianteId,
    string Descripcion,
    decimal PrecioUnitario,
    int Vendida,
    int Devuelta,
    int Disponible);

/// <summary>Una cantidad a devolver de una variante concreta.</summary>
public record DevolucionItemDto(Guid VarianteId, int Cantidad);
