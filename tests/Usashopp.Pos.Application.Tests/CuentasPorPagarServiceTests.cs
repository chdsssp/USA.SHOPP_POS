using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class CuentasPorPagarServiceTests
{
    private readonly ICompraRepository _compras = Substitute.For<ICompraRepository>();
    private readonly IRepository<PagoCompra> _pagos = Substitute.For<IRepository<PagoCompra>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public CuentasPorPagarServiceTests()
    {
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _pagos.ListarAsync(Arg.Any<Expression<Func<PagoCompra, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PagoCompra>());
    }

    private CuentasPorPagarService CrearServicio() =>
        new(_compras, _pagos, _usuario, _reloj, _uow, _auditoria);

    // Compra de total 1000 (10 x 100).
    private Compra CompraTotal1000()
    {
        var compra = new Compra { Folio = "C-1" };
        compra.Detalles.Add(new DetalleCompra { Cantidad = 10, CostoUnitario = new Dinero(100m) });
        _compras.ObtenerConDetalleAsync(compra.Id, Arg.Any<CancellationToken>()).Returns(compra);
        return compra;
    }

    [Fact]
    public async Task RegistrarPago_valido_agrega_abono()
    {
        var compra = CompraTotal1000();
        var r = await CrearServicio().RegistrarPagoAsync(new RegistrarPagoCompraDto(compra.Id, 400m));

        r.Exito.Should().BeTrue();
        await _pagos.Received(1).AgregarAsync(
            Arg.Is<PagoCompra>(p => p.CompraId == compra.Id && p.Monto.Monto == 400m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarPago_monto_no_positivo_falla()
    {
        var compra = CompraTotal1000();
        var r = await CrearServicio().RegistrarPagoAsync(new RegistrarPagoCompraDto(compra.Id, 0m));
        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrarPago_que_excede_el_saldo_falla()
    {
        var compra = CompraTotal1000();
        _pagos.ListarAsync(Arg.Any<Expression<Func<PagoCompra, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PagoCompra> { new() { CompraId = compra.Id, Monto = new Dinero(700m) } });

        // Saldo = 300; intentar pagar 400 debe fallar.
        var r = await CrearServicio().RegistrarPagoAsync(new RegistrarPagoCompraDto(compra.Id, 400m));

        r.EsFallo.Should().BeTrue();
        await _pagos.DidNotReceive().AgregarAsync(Arg.Any<PagoCompra>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EstadoCuenta_calcula_saldo()
    {
        var compra = CompraTotal1000();
        _pagos.ListarAsync(Arg.Any<Expression<Func<PagoCompra, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PagoCompra> { new() { CompraId = compra.Id, Monto = new Dinero(250m) } });

        var estado = await CrearServicio().ObtenerEstadoCuentaAsync(compra.Id);

        estado!.Total.Should().Be(1000m);
        estado.Pagado.Should().Be(250m);
        estado.Saldo.Should().Be(750m);
    }
}
