using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Corte de caja: conteo del efectivo esperado vs. contado y cierre de sesión.</summary>
public partial class CorteCajaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private decimal _fondo;
    [ObservableProperty] private int _numVentas;
    [ObservableProperty] private decimal _totalVentas;
    [ObservableProperty] private decimal _totalEfectivo;
    [ObservableProperty] private decimal _efectivoEsperado;
    [ObservableProperty] private decimal _montoContado;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _sinCaja;

    public decimal Diferencia => MontoContado - EfectivoEsperado;

    public event Action<bool>? Cerrar;

    public CorteCajaViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = CargarAsync();
    }

    partial void OnMontoContadoChanged(decimal value) => OnPropertyChanged(nameof(Diferencia));
    partial void OnEfectivoEsperadoChanged(decimal value) => OnPropertyChanged(nameof(Diferencia));

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
        var corte = await caja.ObtenerCorteAsync();
        if (corte is null) { SinCaja = true; return; }

        Fondo = corte.Fondo;
        NumVentas = corte.NumVentas;
        TotalVentas = corte.TotalVentas;
        TotalEfectivo = corte.TotalEfectivo;
        EfectivoEsperado = corte.EfectivoEsperado;
        MontoContado = corte.EfectivoEsperado;
    }

    [RelayCommand]
    private async Task CerrarCajaAsync()
    {
        Error = null;
        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
        var r = await caja.CerrarAsync(MontoContado);
        if (r.EsFallo) { Error = r.Error; return; }

        WeakReferenceMessenger.Default.Send(new CajaEstadoCambiadoMessage());
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
