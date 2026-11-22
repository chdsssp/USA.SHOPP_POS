using FluentAssertions;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.Services;
using Xunit;

namespace Usashopp.Pos.Domain.Tests;

public class MotorPromocionesTests
{
    private static readonly DateTime Hoy = new(2026, 11, 21);
    private static readonly Guid Cat = Guid.NewGuid();
    private static readonly Guid Prod = Guid.NewGuid();

    private static LineaPromo L(int i, decimal precio, int cant = 1, bool manual = false, Guid? prod = null) =>
        new(i, Guid.NewGuid(), prod ?? Prod, Cat, cant, precio, manual);

    [Fact]
    public void Dos_por_uno_regala_la_pieza_mas_barata_entre_lineas_distintas()
    {
        var p = new Promocion { Nombre = "2x1", Tipo = TipoPromocion.NxM, N = 2, M = 1, Alcance = AlcancePromocion.Categoria, AlcanceId = Cat };
        var r = MotorPromociones.Aplicar(new[] { L(0, 300), L(1, 200) }, new[] { p }, Hoy);

        r.Sum(x => x.Descuento).Should().Be(200m, "se regala la de $200");
        r.Should().OnlyContain(x => x.Nombre == "2x1");
    }

    [Fact]
    public void Tres_por_299_solo_en_paquetes_completos()
    {
        var p = new Promocion { Nombre = "3x299", Tipo = TipoPromocion.NPorPrecio, N = 3, Valor = 299m };
        var r = MotorPromociones.Aplicar(new[] { L(0, 150, 4) }, new[] { p }, Hoy);

        r.Single().Descuento.Should().Be(151m, "3 × 150 = 450 → 299; la cuarta pieza va a precio normal");
    }

    [Fact]
    public void Porcentaje_respeta_vigencia_y_dias_de_la_semana()
    {
        var p = new Promocion { Nombre = "Temporada", Tipo = TipoPromocion.Porcentaje, Valor = 30, Inicio = Hoy.AddDays(1) };
        MotorPromociones.Aplicar(new[] { L(0, 100) }, new[] { p }, Hoy).Should().BeEmpty("aún no empieza");

        p.Inicio = null;
        p.DiasSemana = 1 << (int)DayOfWeek.Monday; // solo lunes; 21/11/2026 es sábado
        MotorPromociones.Aplicar(new[] { L(0, 100) }, new[] { p }, Hoy).Should().BeEmpty();

        p.DiasSemana = 0;
        MotorPromociones.Aplicar(new[] { L(0, 100) }, new[] { p }, Hoy).Single().Descuento.Should().Be(30m);
    }

    [Fact]
    public void Descuento_manual_reemplaza_a_la_promocion_no_acumulable_y_gana_la_de_mayor_prioridad()
    {
        var general = new Promocion { Nombre = "10%", Tipo = TipoPromocion.Porcentaje, Valor = 10, Prioridad = 0 };
        var especial = new Promocion { Nombre = "Precio especial", Tipo = TipoPromocion.PrecioEspecial, Valor = 99, Prioridad = 5,
            Alcance = AlcancePromocion.Producto, AlcanceId = Prod };

        var r = MotorPromociones.Aplicar(new[] { L(0, 199), L(1, 199, manual: true) }, new[] { general, especial }, Hoy);

        r.Should().ContainSingle();
        r[0].Indice.Should().Be(0);
        r[0].Nombre.Should().Be("Precio especial");
        r[0].Descuento.Should().Be(100m);
    }
}
