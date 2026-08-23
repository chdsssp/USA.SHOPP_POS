using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

public partial class UsuarioEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;
    private Guid _rolIdPendiente;

    [ObservableProperty] private string _titulo = "Nuevo usuario";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private RolDto? _rolSeleccionado;
    [ObservableProperty] private bool _esNuevo = true;
    [ObservableProperty] private string? _error;

    public ObservableCollection<RolDto> Roles { get; } = new();

    public event Action<bool>? Cerrar;

    public UsuarioEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(UsuarioDto? usuario)
    {
        if (usuario is null)
        {
            _id = null; EsNuevo = true; Titulo = "Nuevo usuario";
        }
        else
        {
            _id = usuario.Id; EsNuevo = false; Titulo = "Editar usuario";
            Nombre = usuario.Nombre;
            Login = usuario.Login;
            _rolIdPendiente = usuario.RolId;
        }
        _ = CargarRolesAsync();
    }

    private async Task CargarRolesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
        var roles = await servicio.ListarRolesAsync();
        Roles.Clear();
        foreach (var r in roles) Roles.Add(r);
        RolSeleccionado = Roles.FirstOrDefault(r => r.Id == _rolIdPendiente) ?? Roles.FirstOrDefault();
    }

    /// <summary>Guarda el usuario. La contraseña vacía en edición no la cambia.</summary>
    public async Task GuardarAsync(string contrasena)
    {
        Error = null;
        if (RolSeleccionado is null) { Error = "Selecciona un rol."; return; }

        var dto = new GuardarUsuarioDto(
            _id ?? Guid.Empty, Nombre, Login, RolSeleccionado.Id,
            string.IsNullOrWhiteSpace(contrasena) ? null : contrasena);

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
        var r = _id is null ? await servicio.CrearAsync(dto) : await servicio.ActualizarAsync(dto);
        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    public void Cancelar() => Cerrar?.Invoke(false);
}
