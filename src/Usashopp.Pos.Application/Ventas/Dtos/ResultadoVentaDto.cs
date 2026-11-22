namespace Usashopp.Pos.Application.Ventas.Dtos;

/// <summary>
/// Resultado de registrar una venta. <see cref="Advertencias"/> lista problemas no fatales
/// (impresora, cajón) que el cajero debe ver: la venta ya quedó guardada.
/// </summary>
public record ResultadoVentaDto(
    Guid VentaId,
    string Folio,
    decimal Total,
    decimal Cambio,
    IReadOnlyList<PagoResumenDto>? Pagos = null,
    IReadOnlyList<string>? Advertencias = null)
{
    public bool TieneAdvertencias => Advertencias is { Count: > 0 };
}
