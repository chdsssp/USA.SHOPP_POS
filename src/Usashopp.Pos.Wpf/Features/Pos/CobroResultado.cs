using Usashopp.Pos.Application.Ventas.Dtos;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Resultado del diálogo de cobro: los pagos capturados, el cambio y notas opcionales.</summary>
public record CobroResultado(IReadOnlyList<NuevoPagoDto> Pagos, decimal Cambio, string? Notas = null);
