using FluentAssertions;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class VentaTests
{
    private static DetalleVenta Linea(decimal precio, int cantidad, Descuento? desc = null) => new()
    {
        VarianteId = Guid.NewGuid(),
        Descripcion = "Producto",
        Cantidad = cantidad,
        PrecioUnitario = new Dinero(precio),
        Descuento = desc
    };

    [Fact]
    public void Total_suma_las_lineas()
    {
        var venta = new Venta();
        venta.AgregarLinea(Linea(199m, 2)); // 398
        venta.AgregarLinea(Linea(149m, 1)); // 149

        venta.Subtotal.Monto.Should().Be(547m);
        venta.Total.Monto.Should().Be(547m);
    }

    [Fact]
    public void Descuento_global_porcentual_se_aplica_al_total()
    {
        var venta = new Venta { DescuentoGlobal = Descuento.Porcentaje(10) };
        venta.AgregarLinea(Linea(100m, 1));

        venta.Total.Monto.Should().Be(90m);
    }

    [Fact]
    public void Calcula_cambio_cuando_el_pago_excede_el_total()
    {
        var venta = new Venta();
        venta.AgregarLinea(Linea(100m, 1));
        venta.RegistrarPago(new Pago { Metodo = MetodoPago.Efectivo, Monto = new Dinero(200m) });

        venta.EstaPagada.Should().BeTrue();
        venta.Cambio.Monto.Should().Be(100m);
    }

    [Fact]
    public void No_esta_pagada_si_el_pago_es_insuficiente()
    {
        var venta = new Venta();
        venta.AgregarLinea(Linea(100m, 1));
        venta.RegistrarPago(new Pago { Metodo = MetodoPago.Efectivo, Monto = new Dinero(50m) });

        venta.EstaPagada.Should().BeFalse();
    }
}
