using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Proveedores;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Proveedores;

public partial class ProveedoresViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private string _busqueda = string.Empty;
    [ObservableProperty] private ProveedorDto? _seleccionado;

    public ObservableCollection<ProveedorDto> Proveedores { get; } = new();

    public ProveedoresViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ProveedorService>();
        var lista = await servicio.ListarAsync(Busqueda);
        Proveedores.Clear();
        foreach (var p in lista) Proveedores.Add(p);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        if (_dialogos.MostrarEditorProveedor(null)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un proveedor para editar."); return; }
        if (_dialogos.MostrarEditorProveedor(Seleccionado)) await CargarAsync();
    }

    [RelayCommand]
    private async Task DesactivarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un proveedor para eliminar."); return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ProveedorService>();
        var r = await servicio.DesactivarAsync(Seleccionado.Id);
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarAsync();
    }
}
