using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Services;

/// <summary>Desglose de un total en base gravable e impuesto.</summary>
public readonly record struct DesgloseImpuesto(Dinero Base, Dinero Impuesto, Dinero Total);

/// <summary>
/// Calcula el IVA a partir de un total. En esta tienda los precios normalmente ya
/// incluyen el impuesto, así que se despeja desde el total.
/// </summary>
public static class CalculadoraImpuestos
{
    public static DesgloseImpuesto Desglosar(Dinero total, decimal tasa, bool impuestoIncluido)
    {
        if (tasa <= 0)
            return new DesgloseImpuesto(total, Dinero.Cero, total);

        if (impuestoIncluido)
        {
            var baseGravable = new Dinero(total.Monto / (1 + tasa), total.Moneda);
            var impuesto = total.Menos(baseGravable);
            return new DesgloseImpuesto(baseGravable, impuesto, total);
        }

        var imp = new Dinero(total.Monto * tasa, total.Moneda);
        return new DesgloseImpuesto(total, imp, total.Mas(imp));
    }
}
