using FluentAssertions;
using Usashopp.Pos.Domain.Services;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class CalculadoraImpuestosTests
{
    [Fact]
    public void Impuesto_incluido_se_despeja_del_total()
    {
        var d = CalculadoraImpuestos.Desglosar(new Dinero(116m), 0.16m, impuestoIncluido: true);
        d.Base.Monto.Should().Be(100m);
        d.Impuesto.Monto.Should().Be(16m);
        d.Total.Monto.Should().Be(116m);
    }

    [Fact]
    public void Impuesto_agregado_se_suma_al_total()
    {
        var d = CalculadoraImpuestos.Desglosar(new Dinero(100m), 0.16m, impuestoIncluido: false);
        d.Impuesto.Monto.Should().Be(16m);
        d.Total.Monto.Should().Be(116m);
    }

    [Fact]
    public void Sin_tasa_no_hay_impuesto()
    {
        var d = CalculadoraImpuestos.Desglosar(new Dinero(100m), 0m, impuestoIncluido: true);
        d.Impuesto.Monto.Should().Be(0m);
        d.Base.Monto.Should().Be(100m);
    }
}
