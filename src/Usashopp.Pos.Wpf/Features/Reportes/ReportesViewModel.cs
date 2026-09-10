using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Reportes;

public partial class ReportesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;
    private ReporteResumenDto? _ultimo;

    [ObservableProperty] private DateTime _desde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _hasta = DateTime.Today;

    // KPIs
    [ObservableProperty] private decimal _ventasTotal;
    [ObservableProperty] private int _numVentas;
    [ObservableProperty] private decimal _ticketPromedio;
    [ObservableProperty] private int _productosBajoStock;

    // Utilidad
    [ObservableProperty] private decimal _costoTotal;
    [ObservableProperty] private decimal _utilidadBruta;
    [ObservableProperty] private decimal _margenPct;

    // Descuentos / devoluciones
    [ObservableProperty] private decimal _descuentosOtorgados;
    [ObservableProperty] private int _numDevoluciones;
    [ObservableProperty] private decimal _totalDevoluciones;

    // Inventario valorizado
    [ObservableProperty] private decimal _inventarioValorCosto;
    [ObservableProperty] private decimal _inventarioValorPrecio;
    [ObservableProperty] private int _inventarioUnidades;

    // Comparativo
    [ObservableProperty] private decimal _ventasPeriodoAnterior;
    [ObservableProperty] private decimal _variacionPct;

    // Gráficas de barras (valor normalizado por el máximo de cada serie).
    [ObservableProperty] private double _maxCategoria = 1;
    [ObservableProperty] private double _maxMetodo = 1;
    [ObservableProperty] private double _maxHora = 1;

    public ObservableCollection<BarraDto> GraficaCategorias { get; } = new();
    public ObservableCollection<BarraDto> GraficaMetodos { get; } = new();
    public ObservableCollection<BarraDto> GraficaHoras { get; } = new();

    public ObservableCollection<TopProductoDto> TopProductos { get; } = new();
    public ObservableCollection<VentasPorMetodoDto> PorMetodoPago { get; } = new();
    public ObservableCollection<VentasPorUsuarioDto> PorUsuario { get; } = new();
    public ObservableCollection<VentasPorCategoriaDto> PorCategoria { get; } = new();
    public ObservableCollection<VentasPorHoraDto> PorHora { get; } = new();
    public ObservableCollection<ProductoSinMovimientoDto> SinMovimiento { get; } = new();

    public ReportesViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ReportesService>();
        var r = await servicio.ObtenerAsync(Desde, Hasta.AddDays(1).AddTicks(-1));
        _ultimo = r;

        VentasTotal = r.VentasTotal;
        NumVentas = r.NumVentas;
        TicketPromedio = r.TicketPromedio;
        ProductosBajoStock = r.ProductosBajoStock;

        CostoTotal = r.CostoTotal;
        UtilidadBruta = r.UtilidadBruta;
        MargenPct = r.MargenPct;

        DescuentosOtorgados = r.DescuentosOtorgados;
        NumDevoluciones = r.NumDevoluciones;
        TotalDevoluciones = r.TotalDevoluciones;

        InventarioValorCosto = r.InventarioValorCosto;
        InventarioValorPrecio = r.InventarioValorPrecio;
        InventarioUnidades = r.InventarioUnidades;

        VentasPeriodoAnterior = r.VentasPeriodoAnterior;
        VariacionPct = r.VariacionPct;

        Reemplazar(TopProductos, r.TopProductos);
        Reemplazar(PorMetodoPago, r.PorMetodoPago);
        Reemplazar(PorUsuario, r.PorUsuario);
        Reemplazar(PorCategoria, r.PorCategoria);
        Reemplazar(PorHora, r.PorHora);
        Reemplazar(SinMovimiento, r.SinMovimiento);

        // Gráficas
        Reemplazar(GraficaCategorias, r.PorCategoria
            .Select(c => new BarraDto(c.Categoria, (double)c.Importe, c.Importe.ToString("C0", CultureInfo.CurrentCulture))).ToList());
        MaxCategoria = Maximo(GraficaCategorias);
        Reemplazar(GraficaMetodos, r.PorMetodoPago
            .Select(m => new BarraDto(m.Metodo, (double)m.Total, m.Total.ToString("C0", CultureInfo.CurrentCulture))).ToList());
        MaxMetodo = Maximo(GraficaMetodos);
        Reemplazar(GraficaHoras, r.PorHora
            .Select(h => new BarraDto(h.Franja, (double)h.Total, h.Total.ToString("C0", CultureInfo.CurrentCulture))).ToList());
        MaxHora = Maximo(GraficaHoras);
    }

    private static double Maximo(IEnumerable<BarraDto> barras)
    {
        var max = barras.Select(b => b.Valor).DefaultIfEmpty(0).Max();
        return max > 0 ? max : 1;
    }

    private static void Reemplazar<T>(ObservableCollection<T> destino, IReadOnlyList<T> origen)
    {
        destino.Clear();
        foreach (var x in origen) destino.Add(x);
    }

    [RelayCommand]
    private void ExportarCsv()
    {
        var ruta = _dialogos.GuardarComoCsv($"reporte_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.csv");
        if (string.IsNullOrWhiteSpace(ruta)) return;

        try
        {
            File.WriteAllText(ruta, ConstruirCsv(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            _dialogos.Mensaje($"Reporte exportado a:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo exportar: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExportarExcel()
    {
        if (_ultimo is null) return;
        var ruta = _dialogos.GuardarComoArchivo(
            $"reporte_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.xlsx",
            "Libro de Excel (*.xlsx)|*.xlsx", ".xlsx");
        if (string.IsNullOrWhiteSpace(ruta)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var exportador = scope.ServiceProvider.GetRequiredService<IReporteExportador>();
            File.WriteAllBytes(ruta, exportador.ExcelReporte(_ultimo, Desde, Hasta));
            _dialogos.Mensaje($"Reporte exportado a:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo exportar a Excel: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExportarPdf()
    {
        if (_ultimo is null) return;
        var ruta = _dialogos.GuardarComoArchivo(
            $"reporte_{Desde:yyyyMMdd}_{Hasta:yyyyMMdd}.pdf",
            "Documento PDF (*.pdf)|*.pdf", ".pdf");
        if (string.IsNullOrWhiteSpace(ruta)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var exportador = scope.ServiceProvider.GetRequiredService<IReporteExportador>();
            File.WriteAllBytes(ruta, exportador.PdfReporte(_ultimo, Desde, Hasta));
            _dialogos.Mensaje($"Reporte exportado a:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo exportar a PDF: {ex.Message}");
        }
    }

    private string ConstruirCsv()
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        string N(decimal v) => v.ToString(ci);

        sb.AppendLine($"Reporte del periodo,{Desde:yyyy-MM-dd},al,{Hasta:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("Indicador,Valor");
        sb.AppendLine($"Ventas del periodo,{N(VentasTotal)}");
        sb.AppendLine($"Número de ventas,{NumVentas.ToString(ci)}");
        sb.AppendLine($"Ticket promedio,{N(TicketPromedio)}");
        sb.AppendLine($"Ventas periodo anterior,{N(VentasPeriodoAnterior)}");
        sb.AppendLine($"Variación %,{N(Math.Round(VariacionPct * 100, 1))}");
        sb.AppendLine($"Costo de lo vendido,{N(CostoTotal)}");
        sb.AppendLine($"Utilidad bruta,{N(UtilidadBruta)}");
        sb.AppendLine($"Margen %,{N(Math.Round(MargenPct * 100, 1))}");
        sb.AppendLine($"Descuentos otorgados,{N(DescuentosOtorgados)}");
        sb.AppendLine($"Devoluciones/cancelaciones (núm),{NumDevoluciones.ToString(ci)}");
        sb.AppendLine($"Devoluciones/cancelaciones (monto),{N(TotalDevoluciones)}");
        sb.AppendLine($"Inventario valor a costo,{N(InventarioValorCosto)}");
        sb.AppendLine($"Inventario valor a precio,{N(InventarioValorPrecio)}");
        sb.AppendLine($"Inventario unidades,{InventarioUnidades.ToString(ci)}");
        sb.AppendLine($"Productos bajo stock,{ProductosBajoStock.ToString(ci)}");

        Seccion(sb, "Forma de pago,Núm. pagos,Total", PorMetodoPago, m => $"{Esc(m.Metodo)},{m.NumPagos.ToString(ci)},{N(m.Total)}");
        Seccion(sb, "Vendedor,Núm. ventas,Total", PorUsuario, u => $"{Esc(u.Usuario)},{u.NumVentas.ToString(ci)},{N(u.Total)}");
        Seccion(sb, "Categoría,Unidades,Importe", PorCategoria, c => $"{Esc(c.Categoria)},{c.Unidades.ToString(ci)},{N(c.Importe)}");
        Seccion(sb, "Franja horaria,Núm. ventas,Total", PorHora, h => $"{Esc(h.Franja)},{h.NumVentas.ToString(ci)},{N(h.Total)}");
        Seccion(sb, "Producto,Cantidad,Importe", TopProductos, t => $"{Esc(t.Descripcion)},{t.Cantidad.ToString(ci)},{N(t.Importe)}");
        Seccion(sb, "Sin movimiento,SKU,Stock", SinMovimiento, s => $"{Esc(s.Producto)},{Esc(s.Sku)},{s.Stock.ToString(ci)}");

        return sb.ToString();
    }

    private static void Seccion<T>(StringBuilder sb, string encabezado, IEnumerable<T> filas, Func<T, string> fila)
    {
        sb.AppendLine();
        sb.AppendLine(encabezado);
        foreach (var f in filas) sb.AppendLine(fila(f));
    }

    private static string Esc(string campo)
    {
        if (campo.Contains(',') || campo.Contains('"') || campo.Contains('\n') || campo.Contains('\r'))
            return "\"" + campo.Replace("\"", "\"\"") + "\"";
        return campo;
    }
}
