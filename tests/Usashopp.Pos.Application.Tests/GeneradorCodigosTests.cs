using FluentAssertions;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class GeneradorCodigosTests
{
    [Fact]
    public void NuevoSku_es_valido_y_con_prefijo()
    {
        var sku = GeneradorCodigos.NuevoSku();
        sku.Should().StartWith("SKU");
        sku.Length.Should().Be(11);
        new Sku(sku).Valor.Should().Be(sku);
    }

    [Fact]
    public void NuevoSku_es_razonablemente_unico()
    {
        var generados = Enumerable.Range(0, 1000).Select(_ => GeneradorCodigos.NuevoSku()).ToHashSet();
        generados.Count.Should().Be(1000);
    }

    [Fact]
    public void NuevoCodigoBarras_es_ean13_con_digito_de_control_valido()
    {
        var codigo = GeneradorCodigos.NuevoCodigoBarras();
        codigo.Length.Should().Be(13);
        codigo.Should().StartWith("200");
        codigo.Should().MatchRegex("^[0-9]{13}$");

        var d = codigo.Select(c => c - '0').ToArray();
        var suma = 0;
        for (var i = 0; i < 12; i++) suma += d[i] * (i % 2 == 0 ? 1 : 3);
        var control = (10 - suma % 10) % 10;
        d[12].Should().Be(control);
    }
}
