using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Categorias;

/// <summary>Administración de categorías del catálogo (crear, renombrar, eliminar).</summary>
public partial class CategoriasViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private CategoriaDto? _seleccionada;

    public ObservableCollection<CategoriaDto> Categorias { get; } = new();

    public CategoriasViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var lista = await servicio.ListarAsync();
        Categorias.Clear();
        foreach (var c in lista) Categorias.Add(c);
    }

    [RelayCommand]
    private async Task NuevaAsync()
    {
        if (_dialogos.MostrarEditorCategoria(null)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una categoría para editar."); return; }
        if (_dialogos.MostrarEditorCategoria(Seleccionada)) await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una categoría para eliminar."); return; }
        if (!_dialogos.Confirmar($"¿Eliminar la categoría «{Seleccionada.Nombre}»?", "Eliminar categoría")) return;
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var r = await servicio.DesactivarAsync(Seleccionada.Id);
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarAsync();
    }
}
