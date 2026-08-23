using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Proveedores;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Proveedores;

public partial class ProveedorEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;

    [ObservableProperty] private string _titulo = "Nuevo proveedor";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _contacto;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _error;

    public event Action<bool>? Cerrar;

    public ProveedorEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(ProveedorDto? proveedor)
    {
        if (proveedor is null) { _id = null; Titulo = "Nuevo proveedor"; return; }
        _id = proveedor.Id;
        Titulo = "Editar proveedor";
        Nombre = proveedor.Nombre;
        Contacto = proveedor.Contacto;
        Telefono = proveedor.Telefono;
        Email = proveedor.Email;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        var dto = new ProveedorDto(_id ?? Guid.Empty, Nombre, Contacto, Telefono, Email, true);

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ProveedorService>();
        var r = _id is null ? await servicio.CrearAsync(dto) : await servicio.ActualizarAsync(dto);

        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
