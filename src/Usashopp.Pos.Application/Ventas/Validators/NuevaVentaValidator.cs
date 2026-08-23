using FluentValidation;
using Usashopp.Pos.Application.Ventas.Dtos;

namespace Usashopp.Pos.Application.Ventas.Validators;

public class NuevaVentaValidator : AbstractValidator<NuevaVentaDto>
{
    public NuevaVentaValidator()
    {
        RuleFor(v => v.Lineas)
            .NotEmpty().WithMessage("La venta debe tener al menos una línea.");

        RuleForEach(v => v.Lineas).ChildRules(l =>
        {
            l.RuleFor(x => x.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
            l.RuleFor(x => x.DescuentoValor).GreaterThanOrEqualTo(0);
        });

        RuleFor(v => v.Pagos)
            .NotEmpty().WithMessage("Registra al menos un pago.");

        RuleForEach(v => v.Pagos).ChildRules(p =>
            p.RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto del pago debe ser mayor que cero."));
    }
}
