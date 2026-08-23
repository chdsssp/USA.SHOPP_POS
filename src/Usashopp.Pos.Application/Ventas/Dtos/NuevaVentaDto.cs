using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Ventas.Dtos;

public record NuevaLineaDto(
    Guid VarianteId,
    int Cantidad,
    TipoDescuento? DescuentoTipo = null,
    decimal DescuentoValor = 0);

public record NuevoPagoDto(
    MetodoPago Metodo,
    decimal Monto,
    string? Referencia = null);

public record NuevaVentaDto(
    IReadOnlyList<NuevaLineaDto> Lineas,
    IReadOnlyList<NuevoPagoDto> Pagos,
    Guid? ClienteId = null,
    TipoDescuento? DescuentoGlobalTipo = null,
    decimal DescuentoGlobalValor = 0,
    string? Notas = null,
    bool Imprimir = true,
    bool AbrirCajon = true);
