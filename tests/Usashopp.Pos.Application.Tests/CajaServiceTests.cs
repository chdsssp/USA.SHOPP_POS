using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class CajaServiceTests
{
    private readonly ISesionCajaRepository _sesiones = Substitute.For<ISesionCajaRepository>();
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly IRepository<MovimientoCaja> _movCaja = Substitute.For<IRepository<MovimientoCaja>>();
    private readonly IBackupService _backup = Substitute.For<IBackupService>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public CajaServiceTests()
    {
        _usuario.UsuarioId.Returns(Guid.NewGuid());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _movCaja.ListarAsync(Arg.Any<Expression<Func<MovimientoCaja, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<MovimientoCaja>());
    }

    private CajaService CrearServicio() =>
        new(_sesiones, _ventas, _movCaja, _backup, _usuario, _reloj, _uow, _auditoria);

    private static Venta VentaEfectivo(decimal monto)
    {
        var v = new Venta();
        v.Detalles.Add(new DetalleVenta { Cantidad = 1, PrecioUnitario = new Dinero(monto), Descripcion = "X" });
        v.RegistrarPago(new Pago { Metodo = MetodoPago.Efectivo, Monto = new Dinero(monto) });
        v.MarcarPagada();
        return v;
    }

    [Fact]
    public async Task Cerrar_congela_el_resumen_del_corte()
    {
        var sesion = new SesionCaja { FondoInicial = new Dinero(200m) };
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns(sesion);
        _ventas.ListarPorSesionAsync(sesion.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Venta> { VentaEfectivo(100m) });

        var r = await CrearServicio().CerrarAsync(320m);

        r.Exito.Should().BeTrue();
        sesion.CorteNumVentas.Should().Be(1);
        sesion.CorteTotalVentas!.Value.Monto.Should().Be(100m);
        sesion.CorteTotalEfectivo!.Value.Monto.Should().Be(100m);
        sesion.CorteEfectivoEsperado!.Value.Monto.Should().Be(300m); // fondo 200 + efectivo 100
        sesion.MontoContado!.Value.Monto.Should().Be(320m);
    }

    [Fact]
    public async Task Historial_usa_el_corte_congelado_y_no_lo_recalcula()
    {
        // Sesión ya cerrada con snapshot; su venta luego fue cancelada.
        var sesion = new SesionCaja
        {
            FondoInicial = new Dinero(200m),
            CorteNumVentas = 1,
            CorteTotalVentas = new Dinero(100m),
            CorteTotalEfectivo = new Dinero(100m),
            CorteEfectivoEsperado = new Dinero(300m)
        };
        var ventaCancelada = VentaEfectivo(100m);
        ventaCancelada.Cancelar();
        sesion.Ventas.Add(ventaCancelada);
        _sesiones.ListarCerradasAsync(Arg.Any<CancellationToken>()).Returns(new List<SesionCaja> { sesion });

        var cortes = await CrearServicio().ListarCortesAsync();

        // Aunque la venta esté cancelada (recalcular daría 0), el corte congelado se respeta.
        cortes[0].NumVentas.Should().Be(1);
        cortes[0].TotalVentas.Should().Be(100m);
        cortes[0].EfectivoEsperado.Should().Be(300m);
    }

    [Fact]
    public async Task Historial_recalcula_cuando_no_hay_snapshot()
    {
        // Sesión cerrada antes del snapshot (campos nulos): comportamiento heredado.
        var sesion = new SesionCaja { FondoInicial = new Dinero(200m) };
        sesion.Ventas.Add(VentaEfectivo(100m));
        _sesiones.ListarCerradasAsync(Arg.Any<CancellationToken>()).Returns(new List<SesionCaja> { sesion });

        var cortes = await CrearServicio().ListarCortesAsync();

        cortes[0].NumVentas.Should().Be(1);
        cortes[0].TotalVentas.Should().Be(100m);
        cortes[0].EfectivoEsperado.Should().Be(300m);
    }
}
