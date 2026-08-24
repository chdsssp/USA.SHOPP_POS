using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Categorias;

public partial class CategoriaEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;

    [ObservableProperty] private string _titulo = "Nueva categoría";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _descripcion;
    [ObservableProperty] private string? _error;

    public event Action<bool>? Cerrar;

    public CategoriaEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(CategoriaDto? categoria)
    {
        if (categoria is null) { _id = null; Titulo = "Nueva categoría"; return; }
        _id = categoria.Id;
        Titulo = "Editar categoría";
        Nombre = categoria.Nombre;
        Descripcion = categoria.Descripcion;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var r = _id is null
            ? await servicio.CrearAsync(Nombre, Descripcion)
            : await servicio.ActualizarAsync(_id.Value, Nombre, Descripcion);

        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
