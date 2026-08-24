using FluentAssertions;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.Exceptions;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class ApartadoTests
{
    private static Apartado ConTotal(decimal precio, int cantidad)
    {
        var a = new Apartado { ClienteId = Guid.NewGuid() };
        a.Detalles.Add(new DetalleApartado
        {
            VarianteId = Guid.NewGuid(),
            Descripcion = "Producto",
            Cantidad = cantidad,
            PrecioUnitario = new Dinero(precio)
        });
        return a;
    }

    [Fact]
    public void Saldo_es_total_menos_abonos()
    {
        var a = ConTotal(200m, 2); // total 400
        a.Abonos.Add(new AbonoApartado { Monto = new Dinero(150m), Metodo = MetodoPago.Efectivo });

        a.Total.Monto.Should().Be(400m);
        a.TotalAbonado.Monto.Should().Be(150m);
        a.Saldo.Monto.Should().Be(250m);
    }

    [Fact]
    public void No_liquida_con_saldo_pendiente()
    {
        var a = ConTotal(100m, 1);
        var accion = () => a.Liquidar();
        accion.Should().Throw<DomainException>();
    }

    [Fact]
    public void Liquida_cuando_el_saldo_es_cero()
    {
        var a = ConTotal(100m, 1);
        a.Abonos.Add(new AbonoApartado { Monto = new Dinero(100m), Metodo = MetodoPago.Efectivo });

        a.Liquidar();
        a.Estado.Should().Be(EstadoApartado.Liquidado);
    }
}
