using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Roles;

/// <summary>Gestión de roles: alta, edición de permisos y baja.</summary>
public partial class RolesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private RolDetalleDto? _seleccionado;

    public ObservableCollection<RolDetalleDto> Roles { get; } = new();

    public RolesViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<RolService>();
        var lista = await servicio.ListarAsync();
        Roles.Clear();
        foreach (var r in lista) Roles.Add(r);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        if (_dialogos.MostrarEditorRol(null)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un rol para editar."); return; }
        if (Seleccionado.EsSistema) { _dialogos.Mensaje("El rol Administrador no se puede modificar."); return; }
        if (_dialogos.MostrarEditorRol(Seleccionado)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un rol para eliminar."); return; }
        if (!_dialogos.Confirmar($"¿Eliminar el rol «{Seleccionado.Nombre}»?", "Eliminar rol")) return;

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<RolService>();
        var r = await servicio.EliminarAsync(Seleccionado.Id);
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarAsync();
    }
}
