using FluentAssertions;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Infrastructure.Export;
using Xunit;

namespace Usashopp.Pos.Infrastructure.Tests;

public class ReporteExportadorTests
{
    private static ReporteResumenDto Muestra() => new(
        VentasTotal: 12345.67m, NumVentas: 42, TicketPromedio: 293.94m, ProductosBajoStock: 3,
        CostoTotal: 8000m, UtilidadBruta: 4345.67m, MargenPct: 35.2m,
        DescuentosOtorgados: 120m, NumDevoluciones: 2, TotalDevoluciones: 300m,
        InventarioValorCosto: 50000m, InventarioValorPrecio: 90000m, InventarioUnidades: 1200,
        VentasPeriodoAnterior: 10000m, VariacionPct: 23.4m,
        TopProductos: new[] { new TopProductoDto("Playera roja M", 10, 1500m), new TopProductoDto("Gorra", 5, 400m) },
        PorMetodoPago: new[] { new VentasPorMetodoDto("Efectivo", 30, 9000m), new VentasPorMetodoDto("Tarjeta", 12, 3345.67m) },
        PorUsuario: new[] { new VentasPorUsuarioDto("Ana", 20, 6000m) },
        PorCategoria: new[] { new VentasPorCategoriaDto("Ropa", 40, 8000m), new VentasPorCategoriaDto("Accesorios", 15, 4345.67m) },
        PorHora: new[] { new VentasPorHoraDto("12:00–13:00", 8, 2000m) },
        SinMovimiento: new[] { new ProductoSinMovimientoDto("Bufanda", "SKU123", 7) });

    [Fact]
    public void Excel_genera_un_xlsx_no_vacio()
    {
        var bytes = new ReporteExportador().ExcelReporte(Muestra(), DateTime.Today.AddDays(-30), DateTime.Today);
        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(1000);
        // Firma de archivo ZIP (los .xlsx son OpenXML/ZIP: "PK").
        bytes[0].Should().Be((byte)'P');
        bytes[1].Should().Be((byte)'K');
    }

    [Fact]
    public void Pdf_genera_un_documento_no_vacio()
    {
        var bytes = new ReporteExportador().PdfReporte(Muestra(), DateTime.Today.AddDays(-30), DateTime.Today);
        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(1000);
        // Firma de archivo PDF: "%PDF".
        global::System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
