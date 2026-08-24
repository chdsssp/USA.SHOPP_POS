using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Reportes;

public partial class ReportesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private DateTime _desde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _hasta = DateTime.Today;
    [ObservableProperty] private decimal _ventasTotal;
    [ObservableProperty] private int _numVentas;
    [ObservableProperty] private decimal _ticketPromedio;
    [ObservableProperty] private int _productosBajoStock;

    public ObservableCollection<TopProductoDto> TopProductos { get; } = new();
    public ObservableCollection<VentasPorMetodoDto> PorMetodoPago { get; } = new();

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

        VentasTotal = r.VentasTotal;
        NumVentas = r.NumVentas;
        TicketPromedio = r.TicketPromedio;
        ProductosBajoStock = r.ProductosBajoStock;

        TopProductos.Clear();
        foreach (var t in r.TopProductos) TopProductos.Add(t);

        PorMetodoPago.Clear();
        foreach (var m in r.PorMetodoPago) PorMetodoPago.Add(m);
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

    private string ConstruirCsv()
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine($"Reporte del periodo,{Desde:yyyy-MM-dd},al,{Hasta:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("Indicador,Valor");
        sb.AppendLine($"Ventas del periodo,{VentasTotal.ToString(ci)}");
        sb.AppendLine($"Número de ventas,{NumVentas.ToString(ci)}");
        sb.AppendLine($"Ticket promedio,{TicketPromedio.ToString(ci)}");
        sb.AppendLine($"Productos bajo stock,{ProductosBajoStock.ToString(ci)}");

        sb.AppendLine();
        sb.AppendLine("Forma de pago,Núm. pagos,Total");
        foreach (var m in PorMetodoPago)
            sb.AppendLine($"{Escapar(m.Metodo)},{m.NumPagos.ToString(ci)},{m.Total.ToString(ci)}");

        sb.AppendLine();
        sb.AppendLine("Producto,Cantidad,Importe");
        foreach (var t in TopProductos)
            sb.AppendLine($"{Escapar(t.Descripcion)},{t.Cantidad.ToString(ci)},{t.Importe.ToString(ci)}");

        return sb.ToString();
    }

    /// <summary>Entrecomilla y escapa un campo para CSV si contiene comas, comillas o saltos.</summary>
    private static string Escapar(string campo)
    {
        if (campo.Contains(',') || campo.Contains('"') || campo.Contains('\n') || campo.Contains('\r'))
            return "\"" + campo.Replace("\"", "\"\"") + "\"";
        return campo;
    }
}
