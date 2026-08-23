using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Apartados;
using Usashopp.Pos.Application.Apartados.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Apartados;

public partial class ApartadosViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private ApartadoResumenDto? _seleccionado;
    [ObservableProperty] private ApartadoDetalleDto? _detalle;

    public ObservableCollection<ApartadoResumenDto> Apartados { get; } = new();

    public ApartadosViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    partial void OnSeleccionadoChanged(ApartadoResumenDto? value) => _ = CargarDetalleAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
        var lista = await servicio.ListarAsync();
        Apartados.Clear();
        foreach (var a in lista) Apartados.Add(a);
        Detalle = null;
    }

    private async Task CargarDetalleAsync()
    {
        if (Seleccionado is null) { Detalle = null; return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
        Detalle = await servicio.ObtenerDetalleAsync(Seleccionado.Id);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        if (_dialogos.MostrarEditorApartado()) await CargarAsync();
    }

    [RelayCommand]
    private async Task AbonarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un apartado."); return; }
        if (_dialogos.MostrarAbono(Seleccionado.Id, Seleccionado.Folio, Seleccionado.Saldo))
            await CargarAsync();
    }

    [RelayCommand]
    private async Task LiquidarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un apartado."); return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
        var r = await servicio.LiquidarAsync(Seleccionado.Id);
        _dialogos.Mensaje(r.Exito ? "Apartado liquidado." : r.Error!);
        if (r.Exito) await CargarAsync();
    }

    [RelayCommand]
    private async Task CancelarApartadoAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un apartado."); return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
        var r = await servicio.CancelarAsync(Seleccionado.Id);
        _dialogos.Mensaje(r.Exito ? "Apartado cancelado; se devolvió el stock." : r.Error!);
        if (r.Exito) await CargarAsync();
    }
}
