using FluentAssertions;
using Usashopp.Pos.Application.Common;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class BusquedaTextoTests
{
    [Theory]
    [InlineData("camisa", "Camisa")]          // insensible a mayúsculas
    [InlineData("nino", "Niño")]              // insensible a acentos/ñ
    [InlineData("roja", "Playera roja")]      // coincidencia parcial
    public void Coincide_una_palabra(string busqueda, string campo)
    {
        BusquedaTexto.Coincide(BusquedaTexto.Tokens(busqueda), campo).Should().BeTrue();
    }

    [Fact]
    public void Coincide_varias_palabras_en_distintos_campos()
    {
        var tokens = BusquedaTexto.Tokens("camisa azul");
        // "camisa" está en el nombre y "azul" en el color: ambas deben encontrarse.
        BusquedaTexto.Coincide(tokens, "Camisa de vestir", "Marca", "G", "Azul").Should().BeTrue();
    }

    [Fact]
    public void Coincide_busca_en_codigo_o_sku()
    {
        var tokens = BusquedaTexto.Tokens("7501");
        BusquedaTexto.Coincide(tokens, "Camisa", null, null, null, "SKU-1", "7501234567").Should().BeTrue();
    }

    [Fact]
    public void No_coincide_cuando_falta_una_palabra()
    {
        var tokens = BusquedaTexto.Tokens("camisa verde");
        BusquedaTexto.Coincide(tokens, "Camisa", "Marca", "G", "Azul").Should().BeFalse();
    }

    [Fact]
    public void Sin_texto_coincide_siempre()
    {
        BusquedaTexto.Coincide(BusquedaTexto.Tokens("  "), "lo que sea").Should().BeTrue();
    }
}
