using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Login;

/// <summary>
/// Inicio de sesión con usuario y contraseña, o con PIN (cambio rápido de cajero con el dedo).
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISesionManager _sesion;

    [ObservableProperty] private string _usuario = "";
    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsarContrasena))]
    private bool _usarPin;

    public bool UsarContrasena
    {
        get => !UsarPin;
        set => UsarPin = !value;
    }

    /// <summary>Se dispara cuando el acceso fue correcto.</summary>
    public event Action? LoginExitoso;

    public LoginViewModel(IServiceScopeFactory scopeFactory, ISesionManager sesion)
    {
        _scopeFactory = scopeFactory;
        _sesion = sesion;
        _ = ElegirModoAsync();
    }

    /// <summary>Si alguien ya configuró su PIN, el login abre directo en el teclado de PIN.</summary>
    private async Task ElegirModoAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            UsarPin = await scope.ServiceProvider.GetRequiredService<AutenticacionService>().HayUsuariosConPinAsync();
        }
        catch { /* sin BD lista todavía: se queda en usuario y contraseña */ }
    }

    public async Task IntentarAsync(string contrasena)
    {
        if (Ocupado) return;
        Error = null;
        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AutenticacionService>();
            var r = UsarPin ? await auth.ValidarPinAsync(Pin) : await auth.ValidarAsync(Usuario, contrasena);
            if (r.EsFallo) { Error = r.Error; Pin = string.Empty; return; }

            var s = r.Valor!;
            _sesion.IniciarSesion(s.Id, s.Nombre, s.Permisos);
            LoginExitoso?.Invoke();
        }
        finally { Ocupado = false; }
    }
}
