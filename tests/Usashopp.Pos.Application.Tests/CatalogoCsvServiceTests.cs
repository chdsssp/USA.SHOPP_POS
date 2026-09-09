using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class CatalogoCsvServiceTests
{
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IProductoRepository _productos = Substitute.For<IProductoRepository>();
    private readonly IRepository<Categoria> _categorias = Substitute.For<IRepository<Categoria>>();
    private readonly IMovimientoInventarioRepository _movimientos = Substitute.For<IMovimientoInventarioRepository>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public CatalogoCsvServiceTests()
    {
        _categorias.ListarAsync(Arg.Any<Expression<Func<Categoria, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Categoria>());
        _productos.ListarAsync(Arg.Any<Expression<Func<Producto, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Producto>());
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private CatalogoCsvService CrearServicio() =>
        new(_variantes, _productos, _categorias, _movimientos, _usuario, _reloj, _uow, _auditoria);

    [Fact]
    public async Task Importar_fila_nueva_crea_producto_y_variante()
    {
        var csv = "Producto,Categoria,SKU,Precio,Stock\nPlayera,Ropa,ABC123,100,5\n";
        var r = await CrearServicio().ImportarAsync(csv);

        r.Exito.Should().BeTrue();
        r.Valor!.Creados.Should().Be(1);
        r.Valor.Actualizados.Should().Be(0);
        await _productos.Received(1).AgregarAsync(Arg.Is<Producto>(p => p.Nombre == "Playera"), Arg.Any<CancellationToken>());
        await _variantes.Received(1).AgregarAsync(
            Arg.Is<VarianteProducto>(v => v.Sku.Valor == "ABC123" && v.PrecioVenta.Monto == 100m), Arg.Any<CancellationToken>());
        // Stock inicial genera un movimiento.
        await _movimientos.Received(1).AgregarAsync(Arg.Any<MovimientoInventario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Importar_sku_existente_actualiza_precio()
    {
        var existente = new VarianteProducto { Sku = new Sku("XYZ"), PrecioVenta = new Dinero(100m) };
        _variantes.ObtenerPorSkuAsync("XYZ", Arg.Any<CancellationToken>()).Returns(existente);

        var csv = "Producto,SKU,Precio\nPlayera,XYZ,150\n";
        var r = await CrearServicio().ImportarAsync(csv);

        r.Exito.Should().BeTrue();
        r.Valor!.Actualizados.Should().Be(1);
        r.Valor.Creados.Should().Be(0);
        existente.PrecioVenta.Monto.Should().Be(150m);
        await _productos.DidNotReceive().AgregarAsync(Arg.Any<Producto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Importar_precio_invalido_omite_la_fila()
    {
        var csv = "Producto,SKU,Precio\nPlayera,ABC,noEsNumero\n";
        var r = await CrearServicio().ImportarAsync(csv);

        r.Exito.Should().BeTrue();
        r.Valor!.Omitidos.Should().Be(1);
        r.Valor.Creados.Should().Be(0);
    }

    [Fact]
    public async Task Importar_sin_columnas_requeridas_falla()
    {
        var r = await CrearServicio().ImportarAsync("Foo,Bar\n1,2\n");
        r.EsFallo.Should().BeTrue();
    }
}
