using FluentAssertions;
using Usashopp.Pos.Domain.Services;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class OrdenTallasTests
{
    [Fact]
    public void Letras_en_orden_logico_y_mexicanas()
    {
        OrdenTallas.Ordenar(new[] { "XL", "M", "CH", "G", "XS", "EG" })
            .Should().Equal("XS", "CH", "M", "G", "XL", "EG");
    }

    [Fact]
    public void Numericas_de_menor_a_mayor_despues_de_letras()
    {
        OrdenTallas.Ordenar(new[] { "32", "28", "M", "30", "22.5" })
            .Should().Equal("M", "22.5", "28", "30", "32");
    }
}
