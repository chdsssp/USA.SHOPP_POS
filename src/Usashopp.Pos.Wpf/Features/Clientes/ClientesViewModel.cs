using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class ClientesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private string _busqueda = string.Empty;
    [ObservableProperty] private ClienteDto? _seleccionado;

    public ObservableCollection<ClienteDto> Clientes { get; } = new();

    public ClientesViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteService>();
        var lista = await servicio.ListarAsync(Busqueda);
        Clientes.Clear();
        foreach (var c in lista) Clientes.Add(c);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        if (_dialogos.MostrarEditorCliente(null)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un cliente para editar."); return; }
        if (_dialogos.MostrarEditorCliente(Seleccionado)) await CargarAsync();
    }

    [RelayCommand]
    private async Task DesactivarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un cliente para eliminar."); return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteService>();
        var r = await servicio.DesactivarAsync(Seleccionado.Id);
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarAsync();
    }
}
