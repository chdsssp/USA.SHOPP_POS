using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

public partial class UsuariosViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private UsuarioDto? _seleccionado;

    public ObservableCollection<UsuarioDto> Usuarios { get; } = new();

    public UsuariosViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
        var lista = await servicio.ListarAsync();
        Usuarios.Clear();
        foreach (var u in lista) Usuarios.Add(u);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        if (_dialogos.MostrarEditorUsuario(null)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un usuario para editar."); return; }
        if (_dialogos.MostrarEditorUsuario(Seleccionado)) await CargarAsync();
    }

    [RelayCommand]
    private async Task DesactivarAsync()
    {
        if (Seleccionado is null) { _dialogos.Mensaje("Selecciona un usuario para eliminar."); return; }
        if (!_dialogos.Confirmar($"¿Eliminar al usuario «{Seleccionado.Nombre}»?", "Eliminar usuario")) return;
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
        var r = await servicio.DesactivarAsync(Seleccionado.Id);
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarAsync();
    }
}
