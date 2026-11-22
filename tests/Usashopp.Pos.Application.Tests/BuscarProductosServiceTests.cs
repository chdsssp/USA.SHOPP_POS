using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class BuscarProductosServiceTests
{
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IAlmacenImagenes _imagenes = Substitute.For<IAlmacenImagenes>();

    private static VarianteProducto V(Producto p, string talla, string color, decimal precio, int stock)
    {
        var v = new VarianteProducto { Producto = p, ProductoId = p.Id, Talla = talla, Color = color,
            Sku = new Sku($"{p.Nombre}-{talla}-{color}"), PrecioVenta = new Dinero(precio) };
        v.EstablecerStock(stock);
        return v;
    }

    private BuscarProductosService Servicio(params VarianteProducto[] vs)
    {
        _variantes.ListarParaVentaAsync(Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(vs.ToList());
        return new BuscarProductosService(_variantes, _imagenes);
    }

    [Fact]
    public async Task Grid_agrupa_variantes_en_una_tarjeta_por_producto()
    {
        var playera = new Producto { Nombre = "Playera" };
        var gorra = new Producto { Nombre = "Gorra" };
        var s = Servicio(V(playera, "M", "Negro", 199, 3), V(playera, "L", "Negro", 219, 0), V(playera, "M", "Blanco", 199, 2),
                         V(gorra, "", "", 150, 5));

        var pagina = await s.ParaGridProductosAsync();

        pagina.Total.Should().Be(2);
        var p = pagina.Items.Single(i => i.Nombre == "Playera");
        p.NumVariantes.Should().Be(3);
        p.StockTotal.Should().Be(5);
        p.PrecioMin.Should().Be(199);
        p.PrecioMax.Should().Be(219);
        p.VarianteUnica.Should().BeNull();
        pagina.Items.Single(i => i.Nombre == "Gorra").VarianteUnica.Should().NotBeNull("una sola variante se agrega directo");
    }

    [Fact]
    public async Task Matriz_ordena_tallas_y_marca_celdas_inexistentes()
    {
        var playera = new Producto { Nombre = "Playera" };
        var s = Servicio(V(playera, "G", "Negro", 199, 1), V(playera, "CH", "Negro", 199, 1), V(playera, "M", "Blanco", 199, 0));

        var m = await s.ObtenerMatrizAsync(playera.Id);

        m!.Tallas.Should().Equal("CH", "M", "G");
        m.Colores.Should().Equal("Blanco", "Negro");
        m.Filas[0][0].Existe.Should().BeFalse();       // Blanco CH no existe
        m.Filas[0][1].Agotada.Should().BeTrue();       // Blanco M sin existencia
        m.Filas[1][2].Variante!.Stock.Should().Be(1);  // Negro G
    }
}
