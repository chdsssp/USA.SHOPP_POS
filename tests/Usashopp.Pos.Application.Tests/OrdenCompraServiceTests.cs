using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class OrdenCompraServiceTests
{
    private readonly ICompraRepository _compras = Substitute.For<ICompraRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly IConfiguracionTiendaRepository _config = Substitute.For<IConfiguracionTiendaRepository>();
    private readonly IRepository<Proveedor> _proveedores = Substitute.For<IRepository<Proveedor>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public OrdenCompraServiceTests()
    {
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _config.ObtenerAsync(Arg.Any<CancellationToken>()).Returns(new ConfiguracionTienda());
        _proveedores.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new Proveedor { Nombre = "Prov" });
    }

    private OrdenCompraService CrearServicio() =>
        new(_compras, _variantes, _movimientos, _config, _proveedores, _usuario, _reloj, _uow, _auditoria);

    [Fact]
    public async Task CrearOrden_crea_como_ordenada_sin_ingresar_stock()
    {
        var varId = Guid.NewGuid();
        var variante = new VarianteProducto();
        _variantes.ObtenerPorIdAsync(varId, Arg.Any<CancellationToken>()).Returns(variante);
        var servicio = CrearServicio();

        var r = await servicio.CrearOrdenAsync(new NuevaCompraDto(
            Guid.NewGuid(), new[] { new NuevaLineaCompraDto(varId, 10, 50m) }));

        r.Exito.Should().BeTrue();
        await _compras.Received(1).AgregarAsync(
            Arg.Is<Compra>(c => c.Estado == EstadoCompra.Ordenada && c.Detalles.Count == 1),
            Arg.Any<CancellationToken>());
        // No ingresa stock ni movimientos al crear la orden.
        await _movimientos.DidNotReceive().AgregarAsync(Arg.Any<MovimientoInventario>(), Arg.Any<CancellationToken>());
        variante.StockActual.Should().Be(0);
    }

    [Fact]
    public async Task Recibir_parcial_ingresa_stock_y_marca_parcial()
    {
        var variante = new VarianteProducto();
        var detalle = new DetalleCompra { VarianteId = variante.Id, Cantidad = 10, CostoUnitario = new Dinero(50m) };
        var compra = new Compra { ProveedorId = Guid.NewGuid(), Folio = "C-1" };
        compra.Detalles.Add(detalle);
        compra.MarcarOrdenada();

        _compras.ObtenerConDetalleAsync(compra.Id, Arg.Any<CancellationToken>()).Returns(compra);
        _variantes.ObtenerPorIdAsync(variante.Id, Arg.Any<CancellationToken>()).Returns(variante);
        var servicio = CrearServicio();

        var r = await servicio.RecibirAsync(new RecepcionCompraDto(
            compra.Id, new[] { new RecepcionLineaDto(detalle.Id, 4) }));

        r.Exito.Should().BeTrue();
        variante.StockActual.Should().Be(4);
        detalle.CantidadRecibida.Should().Be(4);
        compra.Estado.Should().Be(EstadoCompra.RecibidaParcial);
        await _movimientos.Received(1).AgregarAsync(
            Arg.Is<MovimientoInventario>(m => m.Cantidad == 4 && m.Tipo == TipoMovimientoInventario.Compra),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recibir_todo_lo_pendiente_marca_recibida_y_capa_el_exceso()
    {
        var variante = new VarianteProducto();
        var detalle = new DetalleCompra { VarianteId = variante.Id, Cantidad = 10, CantidadRecibida = 6, CostoUnitario = new Dinero(50m) };
        var compra = new Compra { Folio = "C-2" };
        compra.Detalles.Add(detalle);
        compra.MarcarOrdenada();

        _compras.ObtenerConDetalleAsync(compra.Id, Arg.Any<CancellationToken>()).Returns(compra);
        _variantes.ObtenerPorIdAsync(variante.Id, Arg.Any<CancellationToken>()).Returns(variante);
        var servicio = CrearServicio();

        // Pide 99 pero solo quedan 4 pendientes -> se reciben 4.
        var r = await servicio.RecibirAsync(new RecepcionCompraDto(
            compra.Id, new[] { new RecepcionLineaDto(detalle.Id, 99) }));

        r.Exito.Should().BeTrue();
        variante.StockActual.Should().Be(4);
        detalle.CantidadRecibida.Should().Be(10);
        compra.Estado.Should().Be(EstadoCompra.Recibida);
    }
}
