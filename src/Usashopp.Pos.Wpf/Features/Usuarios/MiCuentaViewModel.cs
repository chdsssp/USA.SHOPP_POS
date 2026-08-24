using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

/// <summary>"Mi cuenta": permite al usuario autenticado cambiar su propia contraseña.</summary>
public partial class MiCuentaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUser _usuario;

    [ObservableProperty] private string _nombreUsuario = string.Empty;
    [ObservableProperty] private string? _error;

    public event Action<bool>? Cerrar;

    public MiCuentaViewModel(IServiceScopeFactory scopeFactory, ICurrentUser usuario)
    {
        _scopeFactory = scopeFactory;
        _usuario = usuario;
        NombreUsuario = usuario.Nombre ?? "Usuario";
    }

    /// <summary>Las contraseñas viven en los PasswordBox (no se bindean), por eso llegan por parámetro.</summary>
    public async Task GuardarAsync(string actual, string nueva, string confirmar)
    {
        Error = null;

        if (nueva != confirmar)
        {
            Error = "La confirmación no coincide con la nueva contraseña.";
            return;
        }

        if (_usuario.UsuarioId is not { } usuarioId)
        {
            Error = "No hay un usuario autenticado.";
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
        var r = await servicio.CambiarMiContrasenaAsync(usuarioId, actual, nueva);

        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    public void Cancelar() => Cerrar?.Invoke(false);
}
