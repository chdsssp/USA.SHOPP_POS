using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Reportes;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Reportes;

public partial class ReportesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private DateTime _desde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _hasta = DateTime.Today;
    [ObservableProperty] private decimal _ventasTotal;
    [ObservableProperty] private int _numVentas;
    [ObservableProperty] private decimal _ticketPromedio;
    [ObservableProperty] private int _productosBajoStock;

    public ObservableCollection<TopProductoDto> TopProductos { get; } = new();

    public ReportesViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
    }
}
