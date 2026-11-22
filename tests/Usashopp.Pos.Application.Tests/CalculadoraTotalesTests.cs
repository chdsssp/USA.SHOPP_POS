using FluentAssertions;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

/// <summary>Los totales de pantalla deben ser idénticos a los de la Venta que se registra.</summary>
public class CalculadoraTotalesTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();

    [Fact]
    public void Sin_descuentos_suma_precio_por_cantidad()
    {
        var r = CalculadoraTotales.Calcular(new[] { new LineaCalculo(A, 2, 199.90m), new LineaCalculo(B, 1, 50m) });

        r.Subtotal.Should().Be(449.80m);
        r.Total.Should().Be(449.80m);
        r.Articulos.Should().Be(3);
    }

    [Fact]
    public void Descuento_porcentual_de_linea_y_global_coinciden_con_el_dominio()
    {
        var lineas = new[]
        {
            new LineaCalculo(A, 3, 33.33m, TipoDescuento.Porcentaje, 15m),
            new LineaCalculo(B, 1, 99.99m, TipoDescuento.MontoFijo, 10m)
        };
        var r = CalculadoraTotales.Calcular(lineas, TipoDescuento.Porcentaje, 7.5m);

        // La misma venta construida por el dominio (como en RegistrarVentaService).
        var venta = new Venta { DescuentoGlobal = Descuento.Porcentaje(7.5m) };
        venta.AgregarLinea(new DetalleVenta { VarianteId = A, Cantidad = 3, PrecioUnitario = new Dinero(33.33m), Descuento = Descuento.Porcentaje(15m) });
        venta.AgregarLinea(new DetalleVenta { VarianteId = B, Cantidad = 1, PrecioUnitario = new Dinero(99.99m), Descuento = Descuento.Monto(10m) });

        r.Subtotal.Should().Be(venta.Subtotal.Monto);
        r.DescuentoGlobal.Should().Be(venta.TotalDescuentoGlobal.Monto);
        r.Total.Should().Be(venta.Total.Monto);
    }

    [Fact]
    public void Descuento_global_mayor_que_el_subtotal_deja_total_en_cero()
    {
        var r = CalculadoraTotales.Calcular(new[] { new LineaCalculo(A, 1, 100m) }, TipoDescuento.MontoFijo, 500m);
        r.Total.Should().Be(0m);
        r.DescuentoGlobal.Should().Be(100m);
    }

    [Fact]
    public void Porcentaje_mayor_a_100_se_acota()
    {
        var r = CalculadoraTotales.Calcular(new[] { new LineaCalculo(A, 1, 100m, TipoDescuento.Porcentaje, 150m) });
        r.Total.Should().Be(0m);
    }

    [Fact]
    public void Desglosa_iva_incluido()
    {
        var r = CalculadoraTotales.Calcular(new[] { new LineaCalculo(A, 1, 116m) }, tasaImpuesto: 0.16m);
        r.Impuesto.Should().Be(16m);
    }
}
