using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class ProductoServiceTests
{
    private readonly IProductoRepository _productos = Substitute.For<IProductoRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly IRepository<HistorialPrecio> _historial = Substitute.For<IRepository<HistorialPrecio>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public ProductoServiceTests()
    {
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _variantes.ExisteSkuAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private ProductoService CrearServicio() =>
        new(_productos, _variantes, _movimientos, _historial, _usuario, _reloj, _uow);

    private (Producto prod, VarianteProducto variante) ProductoConVariante(decimal precio)
    {
        var variante = new VarianteProducto { Sku = new Sku("ABC"), PrecioVenta = new Dinero(precio) };
        var prod = new Producto { Nombre = "Playera", CategoriaId = Guid.NewGuid() };
        prod.Variantes.Add(variante);
        _productos.ObtenerConVariantesAsync(prod.Id, Arg.Any<CancellationToken>()).Returns(prod);
        return (prod, variante);
    }

    private static NuevoProductoDto DtoCon(VarianteProducto v, Guid categoriaId, decimal precio) => new(
        "Playera", categoriaId,
        new[] { new VarianteEntradaDto("ABC", null, null, null, precio, 0, 0, 0, v.Id) });

    [Fact]
    public async Task Actualizar_registra_historial_cuando_cambia_el_precio()
    {
        var (prod, variante) = ProductoConVariante(100m);
        var r = await CrearServicio().ActualizarAsync(prod.Id, DtoCon(variante, prod.CategoriaId, 120m));

        r.Exito.Should().BeTrue();
        await _historial.Received(1).AgregarAsync(
            Arg.Is<HistorialPrecio>(h => h.PrecioAnterior.Monto == 100m && h.PrecioNuevo.Monto == 120m && h.VarianteId == variante.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Actualizar_no_registra_historial_si_el_precio_no_cambia()
    {
        var (prod, variante) = ProductoConVariante(100m);
        var r = await CrearServicio().ActualizarAsync(prod.Id, DtoCon(variante, prod.CategoriaId, 100m));

        r.Exito.Should().BeTrue();
        await _historial.DidNotReceive().AgregarAsync(Arg.Any<HistorialPrecio>(), Arg.Any<CancellationToken>());
    }
}
