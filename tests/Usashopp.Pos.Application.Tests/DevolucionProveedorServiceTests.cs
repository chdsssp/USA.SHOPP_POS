using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class DevolucionProveedorServiceTests
{
    private readonly ICompraRepository _compras = Substitute.For<ICompraRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public DevolucionProveedorServiceTests()
    {
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _movimientos.ListarPorReferenciaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<MovimientoInventario>());
    }

    private DevolucionProveedorService CrearServicio() =>
        new(_compras, _variantes, _movimientos, _usuario, _reloj, _uow, _auditoria);

    private (Compra compra, VarianteProducto variante) CompraRecibida(int recibido, int stock)
    {
        var variante = new VarianteProducto();
        variante.EstablecerStock(stock);
        var compra = new Compra { Folio = "C-1" };
        compra.Detalles.Add(new DetalleCompra { VarianteId = variante.Id, Cantidad = recibido, CantidadRecibida = recibido });
        _compras.ObtenerConDetalleAsync(compra.Id, Arg.Any<CancellationToken>()).Returns(compra);
        _variantes.ObtenerPorIdAsync(variante.Id, Arg.Any<CancellationToken>()).Returns(variante);
        return (compra, variante);
    }

    [Fact]
    public async Task Devolver_baja_stock_y_registra_movimiento_negativo()
    {
        var (compra, variante) = CompraRecibida(recibido: 10, stock: 10);

        var r = await CrearServicio().EjecutarAsync(new DevolucionProveedorDto(
            compra.Id, new[] { new DevolucionProveedorLineaDto(variante.Id, 4) }));

        r.Exito.Should().BeTrue();
        variante.StockActual.Should().Be(6);
        await _movimientos.Received(1).AgregarAsync(
            Arg.Is<MovimientoInventario>(m => m.Cantidad == -4 && m.Tipo == TipoMovimientoInventario.DevolucionProveedor),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Devolver_se_capa_al_stock_disponible()
    {
        // Recibió 10 pero solo hay 3 en existencia (ya se vendieron 7).
        var (compra, variante) = CompraRecibida(recibido: 10, stock: 3);

        var r = await CrearServicio().EjecutarAsync(new DevolucionProveedorDto(
            compra.Id, new[] { new DevolucionProveedorLineaDto(variante.Id, 9) }));

        r.Exito.Should().BeTrue();
        variante.StockActual.Should().Be(0); // solo se pudieron devolver 3
    }
}
