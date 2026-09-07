using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Ventas;

/// <summary>Devolución parcial/total de mercancía de una venta.</summary>
public partial class DevolucionViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _ventaId;

    [ObservableProperty] private string _titulo = "Devolver mercancía";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _cargando;

    /// <summary>Importe que se reembolsará en efectivo (neto pagado, con descuentos).</summary>
    [ObservableProperty] private decimal _totalReembolso;

    public ObservableCollection<LineaDevolucionEditable> Lineas { get; } = new();

    public event Action<bool>? Cerrar;

    public DevolucionViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid ventaId, string folio)
    {
        _ventaId = ventaId;
        Titulo = $"Devolver mercancía · {folio}";
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
            var lista = await servicio.ObtenerLineasAsync(_ventaId);
            Lineas.Clear();
            foreach (var l in lista)
            {
                var linea = new LineaDevolucionEditable
                {
                    VarianteId = l.VarianteId,
                    Descripcion = l.Descripcion,
                    PrecioUnitario = l.PrecioUnitario,
                    Vendida = l.Vendida,
                    Devuelta = l.Devuelta,
                    Disponible = l.Disponible
                };
                linea.PropertyChanged += LineaCambiada;
                Lineas.Add(linea);
            }
        }
        finally { Cargando = false; }
    }

    private void LineaCambiada(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LineaDevolucionEditable.ADevolver))
            _ = RecalcularReembolsoAsync();
    }

    private async Task RecalcularReembolsoAsync()
    {
        var items = ItemsSeleccionados();
        if (items.Count == 0) { TotalReembolso = 0m; return; }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
        TotalReembolso = await servicio.CalcularReembolsoAsync(_ventaId, items);
    }

    private List<DevolucionItemDto> ItemsSeleccionados() => Lineas
        .Where(l => l.ADevolver > 0)
        .Select(l => new DevolucionItemDto(l.VarianteId, l.ADevolver))
        .ToList();

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        Error = null;
        var items = ItemsSeleccionados();

        if (items.Count == 0) { Error = "Indica al menos una cantidad a devolver."; return; }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
        var r = await servicio.EjecutarAsync(_ventaId, items);
        if (r.EsFallo) { Error = r.Error; return; }

        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
