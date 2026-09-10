using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Domain.Entities;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class ConfiguracionServiceTests
{
    private readonly IConfiguracionTiendaRepository _config = Substitute.For<IConfiguracionTiendaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ConfiguracionService CrearServicio() => new(_config, _uow);

    private ConfiguracionTienda Configurar(bool completada = false)
    {
        var c = new ConfiguracionTienda { ConfiguracionCompletada = completada };
        _config.ObtenerAsync(Arg.Any<CancellationToken>()).Returns(c);
        return c;
    }

    [Fact]
    public async Task RequiereAsistente_true_cuando_no_completada()
    {
        Configurar(completada: false);
        (await CrearServicio().RequiereAsistenteAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task RequiereAsistente_false_cuando_completada()
    {
        Configurar(completada: true);
        (await CrearServicio().RequiereAsistenteAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CompletarAsistente_aplica_datos_y_marca_completada()
    {
        var c = Configurar(completada: false);

        var r = await CrearServicio().CompletarAsistenteAsync(new AsistenteConfiguracionDto(
            "Mi Tienda", "Calle 1", "555", "XAXX010101000", "Gracias", 0.16m, "V-", "A-", "C-"));

        r.Exito.Should().BeTrue();
        c.NombreTienda.Should().Be("Mi Tienda");
        c.ConfiguracionCompletada.Should().BeTrue();
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompletarAsistente_sin_nombre_falla()
    {
        Configurar();
        var r = await CrearServicio().CompletarAsistenteAsync(new AsistenteConfiguracionDto(
            "", null, null, null, null, 0.16m, "V-", "A-", "C-"));
        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Guardar_persiste_logo()
    {
        var c = Configurar(completada: true);
        var r = await CrearServicio().GuardarAsync(new ConfiguracionDto(
            "T", null, null, null, null, 0.16m, true, false, "logo.png"));

        r.Exito.Should().BeTrue();
        c.LogoRuta.Should().Be("logo.png");
    }
}
