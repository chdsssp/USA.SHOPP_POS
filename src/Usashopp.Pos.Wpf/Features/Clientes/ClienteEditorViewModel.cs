using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class ClienteEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;

    [ObservableProperty] private string _titulo = "Nuevo cliente";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _notas;
    [ObservableProperty] private string? _error;

    public event Action<bool>? Cerrar;

    public ClienteEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(ClienteDto? cliente)
    {
        if (cliente is null) { _id = null; Titulo = "Nuevo cliente"; return; }
        _id = cliente.Id;
        Titulo = "Editar cliente";
        Nombre = cliente.Nombre;
        Telefono = cliente.Telefono;
        Email = cliente.Email;
        Notas = cliente.Notas;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        var dto = new ClienteDto(_id ?? Guid.Empty, Nombre, Telefono, Email, Notas, true);

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteService>();
        var r = _id is null ? await servicio.CrearAsync(dto) : await servicio.ActualizarAsync(dto);

        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
