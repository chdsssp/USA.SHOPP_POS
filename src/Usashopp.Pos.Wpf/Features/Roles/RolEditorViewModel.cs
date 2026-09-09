using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Roles;

/// <summary>Editor de rol: nombre + casillas de permisos.</summary>
public partial class RolEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;

    [ObservableProperty] private string _titulo = "Nuevo rol";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<PermisoSeleccionable> Permisos { get; } = new();

    public event Action<bool>? Cerrar;

    public RolEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(RolDetalleDto? rol)
    {
        if (rol is null)
        {
            _id = null; Titulo = "Nuevo rol";
        }
        else
        {
            _id = rol.Id; Titulo = $"Editar rol · {rol.Nombre}";
            Nombre = rol.Nombre;
        }
        _ = CargarPermisosAsync();
    }

    private async Task CargarPermisosAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<RolService>();
        var lista = await servicio.ObtenerPermisosAsync(_id);
        Permisos.Clear();
        foreach (var p in lista)
            Permisos.Add(new PermisoSeleccionable { Clave = p.Clave, Etiqueta = p.Etiqueta, Asignado = p.Asignado });
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Ocupado) return;
        Error = null;

        var claves = Permisos.Where(p => p.Asignado).Select(p => p.Clave).ToList();
        var dto = new GuardarRolDto(_id ?? Guid.Empty, Nombre, claves);

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<RolService>();
            var r = _id is null ? await servicio.CrearAsync(dto) : await servicio.ActualizarAsync(dto);
            if (r.EsFallo) { Error = r.Error; return; }
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
