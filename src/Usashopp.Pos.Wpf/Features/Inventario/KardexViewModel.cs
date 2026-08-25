using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Kardex de una variante: movimientos con saldo, filtros y exportación.</summary>
public partial class KardexViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;
    private List<MovimientoKardexDto> _todos = new();
    private string _sku = "kardex";

    [ObservableProperty] private string _titulo = "Kardex";
    [ObservableProperty] private string _subtitulo = string.Empty;
    [ObservableProperty] private int _stockActual;
    [ObservableProperty] private bool _cargando;

    [ObservableProperty] private DateTime? _desde;
    [ObservableProperty] private DateTime? _hasta;
    /// <summary>0 = todos, 1 = solo entradas (+), 2 = solo salidas (−).</summary>
    [ObservableProperty] private int _filtroTipo;

    public ObservableCollection<MovimientoKardexDto> Movimientos { get; } = new();

    public KardexViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
    }

    public void Inicializar(VarianteInventarioDto variante)
    {
        Titulo = variante.Producto;
        var partes = new[] { variante.Talla, variante.Color, variante.Sku }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        Subtitulo = string.Join(" · ", partes);
        StockActual = variante.Stock;
        _sku = string.IsNullOrWhiteSpace(variante.Sku) ? "kardex" : variante.Sku;
        _ = CargarAsync(variante.VarianteId);
    }

    partial void OnDesdeChanged(DateTime? value) => AplicarFiltros();
    partial void OnHastaChanged(DateTime? value) => AplicarFiltros();
    partial void OnFiltroTipoChanged(int value) => AplicarFiltros();

    private async Task CargarAsync(Guid varianteId)
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
            _todos = (await servicio.ObtenerKardexAsync(varianteId)).ToList();
            AplicarFiltros();
        }
        finally { Cargando = false; }
    }

    private void AplicarFiltros()
    {
        IEnumerable<MovimientoKardexDto> q = _todos;
        if (Desde is { } d) q = q.Where(m => m.Fecha >= d.Date);
        if (Hasta is { } h) q = q.Where(m => m.Fecha < h.Date.AddDays(1));
        q = FiltroTipo switch
        {
            1 => q.Where(m => m.Cantidad > 0),
            2 => q.Where(m => m.Cantidad < 0),
            _ => q
        };

        Movimientos.Clear();
        foreach (var m in q) Movimientos.Add(m);
    }

    [RelayCommand]
    private void ExportarCsv()
    {
        var ruta = _dialogos.GuardarComoCsv($"kardex_{_sku}.csv");
        if (string.IsNullOrWhiteSpace(ruta)) return;

        try
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"Kardex,{Esc(Titulo)},{Esc(Subtitulo)}");
            sb.AppendLine();
            sb.AppendLine("Fecha,Movimiento,Cantidad,Saldo,Motivo");
            foreach (var m in Movimientos)
                sb.AppendLine($"{m.Fecha:yyyy-MM-dd HH:mm},{Esc(m.Tipo)},{m.Cantidad.ToString(ci)},{m.Saldo.ToString(ci)},{Esc(m.Motivo ?? "")}");

            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            _dialogos.Mensaje($"Kardex exportado a:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo exportar: {ex.Message}");
        }
    }

    private static string Esc(string campo)
    {
        if (campo.Contains(',') || campo.Contains('"') || campo.Contains('\n') || campo.Contains('\r'))
            return "\"" + campo.Replace("\"", "\"\"") + "\"";
        return campo;
    }
}
