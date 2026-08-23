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

public record CompraLineaDetalleDto(string Descripcion, int Cantidad, decimal CostoUnitario, decimal Importe);

public record CompraDetalleDto(
    Guid Id,
    string Folio,
    string Proveedor,
    DateTime Fecha,
    decimal Total,
    string Estado,
    IReadOnlyList<CompraLineaDetalleDto> Lineas);
