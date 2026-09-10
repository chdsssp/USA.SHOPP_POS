using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Reportes;

namespace Usashopp.Pos.Infrastructure.Export;

/// <summary>Exporta un reporte a Excel (ClosedXML) y PDF (QuestPDF).</summary>
public class ReporteExportador : IReporteExportador
{
    private static readonly CultureInfo Ci = new("es-MX");

    static ReporteExportador()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string Periodo(DateTime? desde, DateTime? hasta)
    {
        var d = desde?.ToString("dd/MM/yyyy") ?? "inicio";
        var h = hasta?.ToString("dd/MM/yyyy") ?? "hoy";
        return $"{d} — {h}";
    }

    public byte[] ExcelReporte(ReporteResumenDto r, DateTime? desde, DateTime? hasta)
    {
        using var wb = new XLWorkbook();

        var resumen = wb.Worksheets.Add("Resumen");
        resumen.Cell(1, 1).Value = "Reporte del periodo";
        resumen.Cell(1, 2).Value = Periodo(desde, hasta);
        var kpis = new (string, object)[]
        {
            ("Ventas (total)", r.VentasTotal),
            ("Núm. de ventas", r.NumVentas),
            ("Ticket promedio", r.TicketPromedio),
            ("Costo total", r.CostoTotal),
            ("Utilidad bruta", r.UtilidadBruta),
            ("Margen %", r.MargenPct),
            ("Descuentos otorgados", r.DescuentosOtorgados),
            ("Devoluciones (núm.)", r.NumDevoluciones),
            ("Devoluciones (total)", r.TotalDevoluciones),
            ("Inventario (costo)", r.InventarioValorCosto),
            ("Inventario (precio)", r.InventarioValorPrecio),
            ("Inventario (unidades)", r.InventarioUnidades),
            ("Ventas periodo anterior", r.VentasPeriodoAnterior),
            ("Variación %", r.VariacionPct),
            ("Productos bajo stock", r.ProductosBajoStock),
        };
        var fila = 3;
        foreach (var (etiqueta, valor) in kpis)
        {
            resumen.Cell(fila, 1).Value = etiqueta;
            resumen.Cell(fila, 2).Value = XLCellValue.FromObject(valor);
            fila++;
        }
        resumen.Columns().AdjustToContents();

        HojaTabla(wb, "Top productos", new[] { "Descripción", "Cantidad", "Importe" },
            r.TopProductos.Select(p => new object[] { p.Descripcion, p.Cantidad, p.Importe }));
        HojaTabla(wb, "Por forma de pago", new[] { "Método", "Pagos", "Total" },
            r.PorMetodoPago.Select(p => new object[] { p.Metodo, p.NumPagos, p.Total }));
        HojaTabla(wb, "Por usuario", new[] { "Usuario", "Ventas", "Total" },
            r.PorUsuario.Select(p => new object[] { p.Usuario, p.NumVentas, p.Total }));
        HojaTabla(wb, "Por categoría", new[] { "Categoría", "Unidades", "Importe" },
            r.PorCategoria.Select(p => new object[] { p.Categoria, p.Unidades, p.Importe }));
        HojaTabla(wb, "Por hora", new[] { "Franja", "Ventas", "Total" },
            r.PorHora.Select(p => new object[] { p.Franja, p.NumVentas, p.Total }));
        HojaTabla(wb, "Sin movimiento", new[] { "Producto", "SKU", "Stock" },
            r.SinMovimiento.Select(p => new object[] { p.Producto, p.Sku, p.Stock }));

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void HojaTabla(XLWorkbook wb, string nombre, string[] encabezados, IEnumerable<object[]> filas)
    {
        // Nombre de hoja: máx. 31 caracteres, sin caracteres inválidos.
        var ws = wb.Worksheets.Add(nombre.Length > 31 ? nombre[..31] : nombre);
        for (var c = 0; c < encabezados.Length; c++)
        {
            var celda = ws.Cell(1, c + 1);
            celda.Value = encabezados[c];
            celda.Style.Font.Bold = true;
        }
        var fila = 2;
        foreach (var f in filas)
        {
            for (var c = 0; c < f.Length; c++)
                ws.Cell(fila, c + 1).Value = XLCellValue.FromObject(f[c]);
            fila++;
        }
        ws.Columns().AdjustToContents();
    }

    public byte[] PdfReporte(ReporteResumenDto r, DateTime? desde, DateTime? hasta)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(h =>
                {
                    h.Item().Text("Reporte del negocio").FontSize(18).SemiBold();
                    h.Item().Text($"Periodo: {Periodo(desde, hasta)}").FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Resumen").FontSize(13).SemiBold();
                    col.Item().Column(k =>
                    {
                        Kpi(k, "Ventas (total)", r.VentasTotal.ToString("C2", Ci));
                        Kpi(k, "Núm. de ventas", r.NumVentas.ToString(Ci));
                        Kpi(k, "Ticket promedio", r.TicketPromedio.ToString("C2", Ci));
                        Kpi(k, "Utilidad bruta", $"{r.UtilidadBruta.ToString("C2", Ci)}  ({r.MargenPct:0.#}%)");
                        Kpi(k, "Descuentos otorgados", r.DescuentosOtorgados.ToString("C2", Ci));
                        Kpi(k, "Devoluciones", $"{r.NumDevoluciones} · {r.TotalDevoluciones.ToString("C2", Ci)}");
                        Kpi(k, "Inventario (costo/precio)", $"{r.InventarioValorCosto.ToString("C2", Ci)} / {r.InventarioValorPrecio.ToString("C2", Ci)}");
                        Kpi(k, "Vs. periodo anterior", $"{r.VentasPeriodoAnterior.ToString("C2", Ci)}  ({r.VariacionPct:0.#}%)");
                    });

                    TablaPdf(col, "Top productos", new[] { "Descripción", "Cantidad", "Importe" },
                        r.TopProductos.Select(p => new[] { p.Descripcion, p.Cantidad.ToString(Ci), p.Importe.ToString("C2", Ci) }));
                    TablaPdf(col, "Por forma de pago", new[] { "Método", "Pagos", "Total" },
                        r.PorMetodoPago.Select(p => new[] { p.Metodo, p.NumPagos.ToString(Ci), p.Total.ToString("C2", Ci) }));
                    TablaPdf(col, "Por categoría", new[] { "Categoría", "Unidades", "Importe" },
                        r.PorCategoria.Select(p => new[] { p.Categoria, p.Unidades.ToString(Ci), p.Importe.ToString("C2", Ci) }));
                    TablaPdf(col, "Por hora", new[] { "Franja", "Ventas", "Total" },
                        r.PorHora.Select(p => new[] { p.Franja, p.NumVentas.ToString(Ci), p.Total.ToString("C2", Ci) }));
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Kpi(ColumnDescriptor col, string etiqueta, string valor) =>
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(etiqueta).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(220).AlignRight().Text(valor).SemiBold();
        });

    private static void TablaPdf(ColumnDescriptor col, string titulo, string[] encabezados, IEnumerable<string[]> filas)
    {
        var datos = filas.ToList();
        if (datos.Count == 0) return;

        col.Item().Text(titulo).FontSize(13).SemiBold();
        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn();
                cols.RelativeColumn();
            });
            foreach (var h in encabezados)
                tabla.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(h).SemiBold();
            foreach (var f in datos)
                for (var c = 0; c < f.Length; c++)
                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(f[c]);
        });
    }
}
