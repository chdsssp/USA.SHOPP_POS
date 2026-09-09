namespace Usashopp.Pos.Application.Compras.Dtos;

public record NuevaLineaCompraDto(Guid VarianteId, int Cantidad, decimal CostoUnitario);

public record NuevaCompraDto(Guid ProveedorId, IReadOnlyList<NuevaLineaCompraDto> Lineas);

public record CompraResumenDto(
    Guid Id,
    string Folio,
    string Proveedor,
    DateTime Fecha,
    decimal Total,
    string Estado,
    decimal Pagado = 0,
    decimal Saldo = 0);

/// <summary>Un pago/abono registrado a una compra.</summary>
public record PagoCompraDto(DateTime Fecha, decimal Monto, string Metodo, string? Nota);

/// <summary>Estado de cuenta de una compra: total, pagado, saldo y pagos.</summary>
public record EstadoCuentaCompraDto(
    Guid CompraId,
    string Folio,
    string Proveedor,
    decimal Total,
    decimal Pagado,
    decimal Saldo,
    IReadOnlyList<PagoCompraDto> Pagos);

/// <summary>Datos para registrar un abono a una compra.</summary>
public record RegistrarPagoCompraDto(Guid CompraId, decimal Monto, string? Nota = null);

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
