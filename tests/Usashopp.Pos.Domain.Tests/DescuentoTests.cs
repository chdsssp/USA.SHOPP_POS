using FluentAssertions;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class DescuentoTests
{
    [Fact]
    public void Porcentaje_calcula_sobre_la_base()
    {
        Descuento.Porcentaje(10).CalcularSobre(new Dinero(100m)).Monto.Should().Be(10m);
    }

    [Fact]
    public void Monto_fijo_no_supera_la_base()
    {
        Descuento.Monto(50m).CalcularSobre(new Dinero(30m)).Monto.Should().Be(30m);
    }

    [Fact]
    public void No_permite_descuento_negativo()
    {
        var accion = () => Descuento.Monto(-5m);
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void No_permite_porcentaje_mayor_a_cien()
    {
        var accion = () => Descuento.Porcentaje(120m);
        accion.Should().Throw<ArgumentException>();
    }
}
