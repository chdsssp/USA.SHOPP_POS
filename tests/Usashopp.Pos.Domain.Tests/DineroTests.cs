using FluentAssertions;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class DineroTests
{
    [Fact]
    public void Redondea_a_dos_decimales()
    {
        var d = new Dinero(10.005m);
        d.Monto.Should().Be(10.00m); // redondeo bancario (ToEven)
    }

    [Fact]
    public void Sumar_montos_de_la_misma_moneda()
    {
        var total = new Dinero(199m).Mas(new Dinero(149m));
        total.Monto.Should().Be(348m);
    }

    [Fact]
    public void Multiplicar_por_cantidad()
    {
        new Dinero(199m).Por(3).Monto.Should().Be(597m);
    }

    [Fact]
    public void No_permite_operar_monedas_distintas()
    {
        var mxn = new Dinero(100m, "MXN");
        var usd = new Dinero(100m, "USD");
        var accion = () => mxn.Mas(usd);
        accion.Should().Throw<InvalidOperationException>();
    }
}
