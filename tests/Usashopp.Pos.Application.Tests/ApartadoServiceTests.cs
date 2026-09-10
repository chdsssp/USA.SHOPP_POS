using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Apartados;
using Usashopp.Pos.Application.Apartados.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class ApartadoServiceTests
{
    private readonly IApartadoRepository _apartados = Substitute.For<IApartadoRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movInv = Substitute.For<IMovimientoInventarioRepository>();
    private readonly IConfiguracionTiendaRepository _config = Substitute.For<IConfiguracionTiendaRepository>();
    private readonly IRepository<Cliente> _clientes = Substitute.For<IRepository<Cliente>>();
    private readonly ISesionCajaRepository _sesiones = Substitute.For<ISesionCajaRepository>();
    private readonly IRepository<MovimientoCaja> _movCaja = Substitute.For<IRepository<MovimientoCaja>>();
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public ApartadoServiceTests()
    {
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _config.ObtenerAsync(Arg.Any<CancellationToken>()).Returns(new ConfiguracionTienda());
    }

    private ApartadoService CrearServicio() => new(
        _apartados, _variantes, _movInv, _config, _clientes, _sesiones, _movCaja, _ventas,
        _usuario, _reloj, _uow, _auditoria);

    private Apartado ApartadoLiquidable()
    {
        var a = new Apartado { ClienteId = Guid.NewGuid(), Folio = "A-1" };
        a.Detalles.Add(new DetalleApartado { VarianteId = Guid.NewGuid(), Cantidad = 1, PrecioUnitario = new Dinero(100m) });
        a.Abonos.Add(new AbonoApartado { Monto = new Dinero(100m) });
        _apartados.ObtenerConDetalleAsync(a.Id, Arg.Any<CancellationToken>()).Returns(a);
        return a;
    }

    [Fact]
    public async Task Liquidar_sin_caja_falla()
    {
        var a = ApartadoLiquidable();
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns((SesionCaja?)null);

        var r = await CrearServicio().LiquidarAsync(a.Id);

        r.EsFallo.Should().BeTrue();
        await _ventas.DidNotReceive().AgregarAsync(Arg.Any<Venta>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Liquidar_con_caja_genera_venta_y_marca_liquidado()
    {
        var a = ApartadoLiquidable();
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns(new SesionCaja());

        var r = await CrearServicio().LiquidarAsync(a.Id);

        r.Exito.Should().BeTrue();
        a.Estado.Should().Be(EstadoApartado.Liquidado);
        await _ventas.Received(1).AgregarAsync(
            Arg.Is<Venta>(v => v.ClienteId == a.ClienteId && v.Detalles.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Abonar_efectivo_sin_caja_falla()
    {
        var a = new Apartado { Folio = "A-2" };
        a.Detalles.Add(new DetalleApartado { Cantidad = 1, PrecioUnitario = new Dinero(100m) });
        _apartados.ObtenerConDetalleAsync(a.Id, Arg.Any<CancellationToken>()).Returns(a);
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns((SesionCaja?)null);

        var r = await CrearServicio().AbonarAsync(new NuevoAbonoDto(a.Id, 50m, MetodoPago.Efectivo));

        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Abonar_efectivo_con_caja_registra_ingreso()
    {
        var a = new Apartado { Folio = "A-3" };
        a.Detalles.Add(new DetalleApartado { Cantidad = 1, PrecioUnitario = new Dinero(100m) });
        _apartados.ObtenerConDetalleAsync(a.Id, Arg.Any<CancellationToken>()).Returns(a);
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns(new SesionCaja());

        var r = await CrearServicio().AbonarAsync(new NuevoAbonoDto(a.Id, 50m, MetodoPago.Efectivo));

        r.Exito.Should().BeTrue();
        await _movCaja.Received(1).AgregarAsync(
            Arg.Is<MovimientoCaja>(m => m.Tipo == TipoMovimientoCaja.Ingreso && m.Monto.Monto == 50m),
            Arg.Any<CancellationToken>());
    }
}
