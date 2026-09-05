using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Caja.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Movimientos de efectivo de la caja (ingreso, retiro, gasto) y estado actual de la
/// caja (equivale a un "reporte X": corte parcial sin cerrar).
/// </summary>
public partial class MovimientoCajaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Estado de la caja (reporte X)
    [ObservableProperty] private bool _sinCaja;
    [ObservableProperty] private decimal _fondo;
    [ObservableProperty] private decimal _totalEfectivo;
    [ObservableProperty] private decimal _ingresos;
    [ObservableProperty] private decimal _salidas;
    [ObservableProperty] private decimal _efectivoEsperado;

    // Alta de movimiento
    [ObservableProperty] private int _tipoIndex;   // 0 Ingreso, 1 Retiro, 2 Gasto
    [ObservableProperty] private decimal _monto;
    [ObservableProperty] private string? _concepto;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<MovimientoCajaDto> Movimientos { get; } = new();

    public MovimientoCajaViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();

        var corte = await caja.ObtenerCorteAsync();
        if (corte is null) { SinCaja = true; return; }

        SinCaja = false;
        Fondo = corte.Fondo;
        TotalEfectivo = corte.TotalEfectivo;
        Ingresos = corte.Ingresos;
        Salidas = corte.Salidas;
        EfectivoEsperado = corte.EfectivoEsperado;

        var lista = await caja.ListarMovimientosAsync();
        Movimientos.Clear();
        foreach (var m in lista) Movimientos.Add(m);
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (Ocupado) return;
        Error = null;

        var tipo = TipoIndex switch
        {
            1 => TipoMovimientoCaja.Retiro,
            2 => TipoMovimientoCaja.Gasto,
            _ => TipoMovimientoCaja.Ingreso
        };

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
            var r = await caja.RegistrarMovimientoAsync(tipo, Monto, Concepto);
            if (r.EsFallo) { Error = r.Error; return; }

            Monto = 0;
            Concepto = null;
            await CargarAsync();
        }
        finally { Ocupado = false; }
    }
}
