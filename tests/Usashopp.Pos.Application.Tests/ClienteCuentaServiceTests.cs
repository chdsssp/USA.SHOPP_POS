using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class ClienteCuentaServiceTests
{
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly IRepository<Cliente> _clientes = Substitute.For<IRepository<Cliente>>();
    private readonly IRepository<AbonoCliente> _abonos = Substitute.For<IRepository<AbonoCliente>>();
    private readonly IRepository<NotaCredito> _notasRepo = Substitute.For<IRepository<NotaCredito>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    private readonly Guid _clienteId = Guid.NewGuid();

    public ClienteCuentaServiceTests()
    {
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _notasRepo.ListarAsync(Arg.Any<Expression<Func<NotaCredito, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<NotaCredito>());
        _abonos.ListarAsync(Arg.Any<Expression<Func<AbonoCliente, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AbonoCliente>());
    }

    private ClienteCuentaService CrearServicio() =>
        new(_ventas, _clientes, _abonos, new NotaCreditoService(_notasRepo), _usuario, _reloj, _uow, _auditoria);

    private void ConfigurarCliente(decimal limite) =>
        _clientes.ObtenerPorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns(new Cliente { Nombre = "Cli", LimiteCredito = new Dinero(limite) });

    private void VentasConCredito(decimal montoCredito)
    {
        var venta = new Venta();
        venta.RegistrarPago(new Pago { Metodo = MetodoPago.Credito, Monto = new Dinero(montoCredito) });
        _ventas.ListarPorClienteAsync(_clienteId, Arg.Any<CancellationToken>()).Returns(new List<Venta> { venta });
    }

    [Fact]
    public async Task EstadoCredito_calcula_saldo_y_disponible()
    {
        ConfigurarCliente(1000m);
        VentasConCredito(500m);
        _abonos.ListarAsync(Arg.Any<Expression<Func<AbonoCliente, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AbonoCliente> { new() { ClienteId = _clienteId, Monto = new Dinero(200m) } });

        var e = await CrearServicio().ObtenerEstadoCreditoAsync(_clienteId);

        e!.Cargos.Should().Be(500m);
        e.Abonos.Should().Be(200m);
        e.Saldo.Should().Be(300m);
        e.Disponible.Should().Be(700m);
    }

    [Fact]
    public async Task RegistrarAbono_valido_agrega_abono()
    {
        ConfigurarCliente(1000m);
        VentasConCredito(500m);

        var r = await CrearServicio().RegistrarAbonoAsync(new RegistrarAbonoClienteDto(_clienteId, 300m));

        r.Exito.Should().BeTrue();
        await _abonos.Received(1).AgregarAsync(
            Arg.Is<AbonoCliente>(a => a.ClienteId == _clienteId && a.Monto.Monto == 300m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarAbono_que_excede_el_saldo_falla()
    {
        ConfigurarCliente(1000m);
        VentasConCredito(500m); // saldo 500, sin abonos

        var r = await CrearServicio().RegistrarAbonoAsync(new RegistrarAbonoClienteDto(_clienteId, 600m));

        r.EsFallo.Should().BeTrue();
        await _abonos.DidNotReceive().AgregarAsync(Arg.Any<AbonoCliente>(), Arg.Any<CancellationToken>());
    }
}
