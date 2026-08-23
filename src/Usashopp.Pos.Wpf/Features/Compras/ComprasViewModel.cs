using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class ComprasViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private CompraResumenDto? _seleccionada;
    [ObservableProperty] private CompraDetalleDto? _detalle;

    public ObservableCollection<CompraResumenDto> Compras { get; } = new();

    public ComprasViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    partial void OnSeleccionadaChanged(CompraResumenDto? value) => _ = CargarDetalleAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarComprasService>();
        var lista = await servicio.ListarAsync();
        Compras.Clear();
        foreach (var c in lista) Compras.Add(c);
        Detalle = null;
    }

    private async Task CargarDetalleAsync()
    {
        if (Seleccionada is null) { Detalle = null; return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarComprasService>();
        Detalle = await servicio.ObtenerDetalleAsync(Seleccionada.Id);
    }

    [RelayCommand]
    private async Task NuevaCompraAsync()
    {
        if (_dialogos.MostrarEditorCompra()) await CargarAsync();
    }
}
