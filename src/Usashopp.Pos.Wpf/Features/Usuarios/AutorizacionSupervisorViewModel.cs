using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

/// <summary>
/// Diálogo de autorización de supervisor: valida el PIN (rápido, táctil) o el usuario y contraseña
/// de alguien con el permiso requerido para autorizar una acción del cajero actual.
/// </summary>
public partial class AutorizacionSupervisorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private string _permiso = string.Empty;
    private string _accion = string.Empty;

    [ObservableProperty] private string _mensaje = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    /// <summary>true = autorizar con PIN; false = con usuario y contraseña.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsarContrasena))]
    private bool _usarPin = true;

    public bool UsarContrasena
    {
        get => !UsarPin;
        set => UsarPin = !value;
    }

    public event Action<bool>? Cerrar;

    public AutorizacionSupervisorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(string permiso, string accion)
    {
        _permiso = permiso;
        _accion = accion;
        Mensaje = $"Un supervisor autorizado debe validar: {accion}.";
    }

    /// <summary>La contraseña vive en el PasswordBox (no se bindea), por eso llega por parámetro.</summary>
    public async Task AutorizarAsync(string contrasena)
    {
        if (Ocupado) return;
        Error = null;
        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AutenticacionService>();
            var r = UsarPin
                ? await auth.AutorizarConPinAsync(Pin, _permiso, _accion)
                : await auth.AutorizarAsync(Login, contrasena, _permiso, _accion);
            if (r.EsFallo) { Error = r.Error; Pin = string.Empty; return; }
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    public void Cancelar() => Cerrar?.Invoke(false);
}
