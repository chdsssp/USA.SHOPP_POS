using FluentAssertions;
using Usashopp.Pos.Application.Common;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class CsvUtilTests
{
    [Fact]
    public void Escapar_entrecomilla_cuando_hay_coma_o_comillas()
    {
        CsvUtil.Escapar("simple").Should().Be("simple");
        CsvUtil.Escapar("a,b").Should().Be("\"a,b\"");
        CsvUtil.Escapar("dice \"hola\"").Should().Be("\"dice \"\"hola\"\"\"");
        CsvUtil.Escapar(null).Should().Be("");
    }

    [Fact]
    public void Parsear_maneja_comillas_comas_internas_y_saltos()
    {
        var csv = "Nombre,Precio\r\n\"Playera, roja\",100\r\n\"Dice \"\"hola\"\"\",50\n";
        var filas = CsvUtil.Parsear(csv);

        filas.Should().HaveCount(3);
        filas[0].Should().Equal("Nombre", "Precio");
        filas[1].Should().Equal("Playera, roja", "100");
        filas[2].Should().Equal("Dice \"hola\"", "50");
    }

    [Fact]
    public void Parsear_descarta_filas_vacias()
    {
        var filas = CsvUtil.Parsear("a,b\n\n\nc,d\n");
        filas.Should().HaveCount(2);
    }
}
