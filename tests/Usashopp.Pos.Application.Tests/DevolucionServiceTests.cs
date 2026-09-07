using FluentAssertions;
using NSubstitute;
using Xunit;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Tests;

public class DevolucionServiceTests
{
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movInv = Substitute.For<IMovimientoInventarioRepository>();
    private readonly ISesionCajaRepository _sesiones = Substitute.For<ISesionCajaRepository>();
    private readonly IRepository<MovimientoCaja> _movCaja = Substitute.For<IRepository<MovimientoCaja>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly Guid _varA = Guid.NewGuid();
    private readonly Guid _varB = Guid.NewGuid();

    public DevolucionServiceTests()
    {
        _usuario.UsuarioId.Returns(Guid.NewGuid());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        // Ejecuta la operación transaccional en línea.
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _movInv.ListarPorReferenciaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MovimientoInventario>());
        _variantes.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new VarianteProducto());
    }

    private DevolucionService CrearServicio() =>
        new(_ventas, _variantes, _movInv, _sesiones, _movCaja, _usuario, _reloj, _uow);

    // Venta: A x2 @100 con 10% de línea (neto/u = 90), B x1 @50; descuento global 10% (factor 0.9).
    private Venta CrearVenta()
    {
        var venta = new Venta { Folio = "V-001", DescuentoGlobal = Descuento.Porcentaje(10) };
        venta.Detalles.Add(new DetalleVenta
        {
            VarianteId = _varA, Descripcion = "A", Cantidad = 2,
            PrecioUnitario = new Dinero(100m), Descuento = Descuento.Porcentaje(10)
        });
        venta.Detalles.Add(new DetalleVenta
        {
            VarianteId = _varB, Descripcion = "B", Cantidad = 1, PrecioUnitario = new Dinero(50m)
        });
        venta.MarcarPagada();
        return venta;
    }

    [Fact]
    public async Task CalcularReembolso_respeta_descuentos_de_linea_y_global()
    {
        var venta = CrearVenta();
        _ventas.ObtenerConDetalleAsync(venta.Id, Arg.Any<CancellationToken>()).Returns(venta);
        var servicio = CrearServicio();

        // Devolver 1 unidad de A: 90 (neto/u) × 1 × 0.9 (global) = 81.00
        var monto = await servicio.CalcularReembolsoAsync(
            venta.Id, new[] { new DevolucionItemDto(_varA, 1) });

        monto.Should().Be(81.00m);
    }

    [Fact]
    public async Task Ejecutar_sin_caja_abierta_falla_y_no_reembolsa()
    {
        var venta = CrearVenta();
        _ventas.ObtenerConDetalleAsync(venta.Id, Arg.Any<CancellationToken>()).Returns(venta);
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>()).Returns((SesionCaja?)null);
        var servicio = CrearServicio();

        var r = await servicio.EjecutarAsync(venta.Id, new[] { new DevolucionItemDto(_varA, 1) });

        r.EsFallo.Should().BeTrue();
        r.Error.Should().Contain("caja");
        await _movCaja.DidNotReceive().AgregarAsync(Arg.Any<MovimientoCaja>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ejecutar_con_caja_registra_reembolso_neto_y_marca_parcial()
    {
        var venta = CrearVenta();
        _ventas.ObtenerConDetalleAsync(venta.Id, Arg.Any<CancellationToken>()).Returns(venta);
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>())
            .Returns(new SesionCaja { FondoInicial = new Dinero(500m) });
        var servicio = CrearServicio();

        var r = await servicio.EjecutarAsync(venta.Id, new[] { new DevolucionItemDto(_varA, 1) });

        r.Exito.Should().BeTrue();
        r.Valor.Should().Be(81.00m);
        await _movCaja.Received(1).AgregarAsync(
            Arg.Is<MovimientoCaja>(m => m.Tipo == TipoMovimientoCaja.Reembolso && m.Monto.Monto == 81.00m),
            Arg.Any<CancellationToken>());
        venta.Estado.Should().Be(EstadoVenta.ParcialmenteDevuelta);
    }

    [Fact]
    public async Task Ejecutar_devolucion_total_marca_devuelta()
    {
        var venta = CrearVenta();
        _ventas.ObtenerConDetalleAsync(venta.Id, Arg.Any<CancellationToken>()).Returns(venta);
        _sesiones.ObtenerSesionAbiertaAsync(Arg.Any<CancellationToken>())
            .Returns(new SesionCaja { FondoInicial = new Dinero(500m) });
        var servicio = CrearServicio();

        var r = await servicio.EjecutarAsync(venta.Id, new[]
        {
            new DevolucionItemDto(_varA, 2),
            new DevolucionItemDto(_varB, 1)
        });

        r.Exito.Should().BeTrue();
        // Neto total = 207 (Total de la venta con descuentos) reembolsado por completo.
        r.Valor.Should().Be(207.00m);
        venta.Estado.Should().Be(EstadoVenta.Devuelta);
    }
}
