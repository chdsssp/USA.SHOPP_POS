using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Caja.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Cortes;

/// <summary>Historial de cortes de caja (sesiones ya cerradas).</summary>
public partial class HistorialCortesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private bool _cargando;

    public ObservableCollection<CorteHistorialDto> Cortes { get; } = new();

    public HistorialCortesViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
            var lista = await caja.ListarCortesAsync();
            Cortes.Clear();
            foreach (var c in lista) Cortes.Add(c);
        }
        finally { Cargando = false; }
    }
}
