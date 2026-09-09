namespace Usashopp.Pos.Application.Compras.Dtos;

public record NuevaLineaCompraDto(Guid VarianteId, int Cantidad, decimal CostoUnitario);

public record NuevaCompraDto(Guid ProveedorId, IReadOnlyList<NuevaLineaCompraDto> Lineas);

public record CompraResumenDto(
    Guid Id,
    string Folio,
    string Proveedor,
    DateTime Fecha,
    decimal Total,
    string Estado);

public record CompraLineaDetalleDto(
    string Descripcion,
    int Cantidad,
    decimal CostoUnitario,
    decimal Importe,
    Guid DetalleId,
    Guid VarianteId,
    int CantidadRecibida,
    int Pendiente);

public record CompraDetalleDto(
    Guid Id,
    string Folio,
    string Proveedor,
    DateTime Fecha,
    decimal Total,
    string Estado,
    IReadOnlyList<CompraLineaDetalleDto> Lineas);

/// <summary>Cantidad a recibir de una línea de compra.</summary>
public record RecepcionLineaDto(Guid DetalleId, int Cantidad);

/// <summary>Recepción (total o parcial) de una orden de compra.</summary>
public record RecepcionCompraDto(Guid CompraId, IReadOnlyList<RecepcionLineaDto> Lineas);

/// <summary>Cantidad a devolver al proveedor de una variante de la compra.</summary>
public record DevolucionProveedorLineaDto(Guid VarianteId, int Cantidad);

/// <summary>Devolución de mercancía recibida a un proveedor.</summary>
public record DevolucionProveedorDto(Guid CompraId, IReadOnlyList<DevolucionProveedorLineaDto> Lineas);
