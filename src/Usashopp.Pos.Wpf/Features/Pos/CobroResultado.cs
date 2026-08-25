using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Resultado del diálogo de cobro: los pagos capturados y el cambio.</summary>
public record CobroResultado(IReadOnlyList<NuevoPagoDto> Pagos, decimal Cambio);

/// <summary>Un pago ya capturado en el diálogo (para pago mixto).</summary>
public record PagoCapturado(MetodoPago Metodo, decimal Monto)
{
    public string MetodoTexto => Metodo == MetodoPago.Efectivo ? "Efectivo" : "Tarjeta";
}
