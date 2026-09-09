using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class InventarioServiceTests
{
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public InventarioServiceTests()
    {
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private InventarioService CrearServicio() =>
        new(_variantes, _movimientos, _usuario, _reloj, _uow, _auditoria);

    private static VarianteProducto VarianteConStock(int stock)
    {
        var v = new VarianteProducto();
        v.EstablecerStock(stock);
        return v;
    }

    [Fact]
    public async Task TomaFisica_ajusta_solo_las_lineas_con_diferencia()
    {
        var conDiferencia = VarianteConStock(5);
        var sinCambio = VarianteConStock(3);
        _variantes.ObtenerPorIdAsync(conDiferencia.Id, Arg.Any<CancellationToken>()).Returns(conDiferencia);
        _variantes.ObtenerPorIdAsync(sinCambio.Id, Arg.Any<CancellationToken>()).Returns(sinCambio);
        var servicio = CrearServicio();

        var r = await servicio.AplicarTomaFisicaAsync(new[]
        {
            new TomaFisicaLineaDto(conDiferencia.Id, 8), // +3
            new TomaFisicaLineaDto(sinCambio.Id, 3),     // sin cambio
        });

        r.Exito.Should().BeTrue();
        r.Valor!.Ajustadas.Should().Be(1);
        r.Valor.DiferenciaNeta.Should().Be(3);
        conDiferencia.StockActual.Should().Be(8);
        await _movimientos.Received(1).AgregarAsync(
            Arg.Is<MovimientoInventario>(m => m.Cantidad == 3 && m.Tipo == TipoMovimientoInventario.AjustePositivo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TomaFisica_sin_lineas_falla()
    {
        var r = await CrearServicio().AplicarTomaFisicaAsync(Array.Empty<TomaFisicaLineaDto>());
        r.EsFallo.Should().BeTrue();
    }
}
