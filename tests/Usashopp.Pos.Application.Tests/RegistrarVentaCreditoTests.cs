using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Application.Ventas.Validators;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class RegistrarVentaCreditoTests
{
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly ISesionCajaRepository _sesiones = Substitute.For<ISesionCajaRepository>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly IConfiguracionTiendaRepository _config = Substitute.For<IConfiguracionTiendaRepository>();
    private readonly IRepository<Cliente> _clientes = Substitute.For<IRepository<Cliente>>();
    private readonly IRepository<NotaCredito> _notas = Substitute.For<IRepository<NotaCredito>>();
    private readonly IRepository<AbonoCliente> _abonos = Substitute.For<IRepository<AbonoCliente>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ITicketPrinter _impresora = Substitute.For<ITicketPrinter>();
    private readonly ICashDrawer _cajon = Substitute.For<ICashDrawer>();

    private readonly Guid _varId = Guid.NewGuid();
    private readonly Guid _clienteId = Guid.NewGuid();

    public RegistrarVentaCreditoTests()
    {
        _usuario.UsuarioId.Returns(Guid.NewGuid());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns(new SesionCaja());
        _config.ObtenerAsync(Arg.Any<CancellationToken>()).Returns(new ConfiguracionTienda());
        var variante = new VarianteProducto { PrecioVenta = new Dinero(100m) };
        variante.EstablecerStock(10);
        _variantes.ObtenerPorIdAsync(_varId, Arg.Any<CancellationToken>()).Returns(variante);
        _ventas.ListarPorClienteAsync(_clienteId, Arg.Any<CancellationToken>()).Returns(new List<Venta>());
        _abonos.ListarAsync(Arg.Any<Expression<Func<AbonoCliente, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AbonoCliente>());
    }

    private RegistrarVentaService CrearServicio() => new(
        new NuevaVentaValidator(), _variantes, _ventas, _sesiones, _movimientos, _config,
        _clientes, _notas, _abonos, _usuario, _reloj, _uow, _impresora, _cajon);

    private NuevaVentaDto Venta(MetodoPago metodo) => new(
        new[] { new NuevaLineaDto(_varId, 1) },
        new[] { new NuevoPagoDto(metodo, 100m) },
        ClienteId: _clienteId, Imprimir: false, AbrirCajon: false);

    [Fact]
    public async Task Venta_a_credito_dentro_del_limite_se_registra()
    {
        _clientes.ObtenerPorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns(new Cliente { Nombre = "Cli", LimiteCredito = new Dinero(1000m) });

        var r = await CrearServicio().EjecutarAsync(Venta(MetodoPago.Credito));

        r.Exito.Should().BeTrue();
        await _ventas.Received(1).AgregarAsync(Arg.Any<Venta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Venta_a_credito_que_excede_el_limite_falla()
    {
        _clientes.ObtenerPorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns(new Cliente { Nombre = "Cli", LimiteCredito = new Dinero(50m) });

        var r = await CrearServicio().EjecutarAsync(Venta(MetodoPago.Credito));

        r.EsFallo.Should().BeTrue();
        await _ventas.DidNotReceive().AgregarAsync(Arg.Any<Venta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Venta_con_nota_de_credito_consume_el_saldo()
    {
        _clientes.ObtenerPorIdAsync(_clienteId, Arg.Any<CancellationToken>())
            .Returns(new Cliente { Nombre = "Cli" });
        var nota = new NotaCredito { ClienteId = _clienteId, Saldo = new Dinero(100m), Estado = EstadoNotaCredito.Activa };
        _notas.ListarAsync(Arg.Any<Expression<Func<NotaCredito, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<NotaCredito> { nota });

        var r = await CrearServicio().EjecutarAsync(Venta(MetodoPago.NotaCredito));

        r.Exito.Should().BeTrue();
        nota.Saldo.Monto.Should().Be(0m);
        nota.Estado.Should().Be(EstadoNotaCredito.Usada);
    }
}
