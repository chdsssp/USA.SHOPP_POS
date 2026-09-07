using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Auditoria;
using Usashopp.Pos.Application.Auditoria.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Auditoria;

/// <summary>Visor de la bitácora de auditoría (acciones relevantes de los usuarios).</summary>
public partial class AuditoriaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private bool _cargando;
    [ObservableProperty] private DateTime? _desde;
    [ObservableProperty] private DateTime? _hasta;
    [ObservableProperty] private string? _texto;

    public ObservableCollection<RegistroAuditoriaDto> Registros { get; } = new();

    public AuditoriaViewModel(IServiceScopeFactory scopeFactory)
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
            // "Hasta" incluye todo el día seleccionado.
            var hasta = Hasta?.Date.AddDays(1).AddTicks(-1);
            var filtro = new FiltroAuditoriaDto(Desde?.Date, hasta, Texto);

            using var scope = _scopeFactory.CreateScope();
            var auditoria = scope.ServiceProvider.GetRequiredService<AuditoriaService>();
            var lista = await auditoria.ListarAsync(filtro);
            Registros.Clear();
            foreach (var r in lista) Registros.Add(r);
        }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task LimpiarAsync()
    {
        Desde = null;
        Hasta = null;
        Texto = null;
        await CargarAsync();
    }
}
