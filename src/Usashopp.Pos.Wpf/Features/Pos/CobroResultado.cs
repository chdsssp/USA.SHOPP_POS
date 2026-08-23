using Usashopp.Pos.Application.Ventas.Dtos;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Resultado del diálogo de cobro: los pagos capturados y el cambio.</summary>
public record CobroResultado(IReadOnlyList<NuevoPagoDto> Pagos, decimal Cambio);
