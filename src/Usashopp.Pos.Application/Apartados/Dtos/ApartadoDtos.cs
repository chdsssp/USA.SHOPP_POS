using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Apartados.Dtos;

public record NuevaLineaApartadoDto(Guid VarianteId, int Cantidad, decimal PrecioUnitario);

public record NuevoApartadoDto(
    Guid ClienteId,
    IReadOnlyList<NuevaLineaApartadoDto> Lineas,
    decimal AnticipoInicial,
    MetodoPago MetodoAnticipo);

public record NuevoAbonoDto(Guid ApartadoId, decimal Monto, MetodoPago Metodo);

public record ApartadoResumenDto(
    Guid Id,
    string Folio,
    string Cliente,
    DateTime Fecha,
    decimal Total,
    decimal Abonado,
    decimal Saldo,
    string Estado);

public record LineaApartadoDetalleDto(string Descripcion, int Cantidad, decimal PrecioUnitario, decimal Importe);

public record AbonoDetalleDto(DateTime Fecha, decimal Monto, string Metodo);

public record ApartadoDetalleDto(
    Guid Id,
    string Folio,
    string Cliente,
    DateTime Fecha,
    decimal Total,
    decimal Abonado,
    decimal Saldo,
    string Estado,
    IReadOnlyList<LineaApartadoDetalleDto> Lineas,
    IReadOnlyList<AbonoDetalleDto> Abonos);
