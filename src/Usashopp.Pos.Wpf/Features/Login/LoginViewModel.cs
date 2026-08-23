using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Login;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISesionManager _sesion;

    [ObservableProperty] private string _usuario = "admin";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    /// <summary>Se dispara cuando el acceso fue correcto.</summary>
    public event Action? LoginExitoso;

    public LoginViewModel(IServiceScopeFactory scopeFactory, ISesionManager sesion)
    {
        _scopeFactory = scopeFactory;
        _sesion = sesion;
    }

    public async Task IntentarAsync(string contrasena)
    {
        Error = null;
        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AutenticacionService>();
            var r = await auth.ValidarAsync(Usuario, contrasena);
            if (r.EsFallo) { Error = r.Error; return; }

            var s = r.Valor!;
            _sesion.IniciarSesion(s.Id, s.Nombre, s.Permisos);
            LoginExitoso?.Invoke();
        }
        finally { Ocupado = false; }
    }
}
