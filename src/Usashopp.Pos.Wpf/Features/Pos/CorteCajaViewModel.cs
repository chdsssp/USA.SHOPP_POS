using System.Collections.ObjectModel;
using System.ComponentModel;
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
    [ObservableProperty] private decimal _ingresos;
    [ObservableProperty] private decimal _salidas;
    [ObservableProperty] private decimal _efectivoEsperado;
    [ObservableProperty] private decimal _montoContado;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _sinCaja;

    /// <summary>Si el efectivo contado se arma sumando billetes y monedas (solo cálculo, no se guarda el desglose).</summary>
    [ObservableProperty] private bool _contarPorDenominaciones;

    public decimal Diferencia => MontoContado - EfectivoEsperado;

    /// <summary>Denominaciones MXN de mayor a menor (billetes y monedas).</summary>
    public ObservableCollection<DenominacionConteo> Denominaciones { get; } = new(new[]
    {
        new DenominacionConteo { Valor = 1000m, Etiqueta = "$1,000" },
        new DenominacionConteo { Valor = 500m,  Etiqueta = "$500" },
        new DenominacionConteo { Valor = 200m,  Etiqueta = "$200" },
        new DenominacionConteo { Valor = 100m,  Etiqueta = "$100" },
        new DenominacionConteo { Valor = 50m,   Etiqueta = "$50" },
        new DenominacionConteo { Valor = 20m,   Etiqueta = "$20" },
        new DenominacionConteo { Valor = 10m,   Etiqueta = "$10" },
        new DenominacionConteo { Valor = 5m,    Etiqueta = "$5" },
        new DenominacionConteo { Valor = 2m,    Etiqueta = "$2" },
        new DenominacionConteo { Valor = 1m,    Etiqueta = "$1" },
        new DenominacionConteo { Valor = 0.50m, Etiqueta = "50¢" },
    });

    /// <summary>Total contado a partir de las denominaciones.</summary>
    public decimal SumaDenominaciones => Denominaciones.Sum(d => d.Importe);

    public event Action<bool>? Cerrar;

    public CorteCajaViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        foreach (var d in Denominaciones)
            d.PropertyChanged += DenominacionCambiada;
        _ = CargarAsync();
    }

    partial void OnMontoContadoChanged(decimal value) => OnPropertyChanged(nameof(Diferencia));
    partial void OnEfectivoEsperadoChanged(decimal value) => OnPropertyChanged(nameof(Diferencia));

    partial void OnContarPorDenominacionesChanged(bool value)
    {
        // Al activar el conteo por denominaciones, el monto contado lo arma la suma.
        if (value) MontoContado = SumaDenominaciones;
    }

    private void DenominacionCambiada(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DenominacionConteo.Importe)) return;
        OnPropertyChanged(nameof(SumaDenominaciones));
        if (ContarPorDenominaciones) MontoContado = SumaDenominaciones;
    }

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
        Ingresos = corte.Ingresos;
        Salidas = corte.Salidas;
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
