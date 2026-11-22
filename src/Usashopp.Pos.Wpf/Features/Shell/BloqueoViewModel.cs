using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>Pantalla de bloqueo manual: se desbloquea con el PIN (o contraseña) del cajero o de un encargado.</summary>
public partial class BloqueoViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUser _usuario;

    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string? _error;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsarContrasena))]
    private bool _usarPin = true;

    public bool UsarContrasena { get => !UsarPin; set => UsarPin = !value; }

    public string Mensaje => $"Caja bloqueada por {_usuario.Nombre ?? "el cajero"}";

    public event Action? Desbloqueado;

    public BloqueoViewModel(IServiceScopeFactory scopeFactory, ICurrentUser usuario)
    {
        _scopeFactory = scopeFactory;
        _usuario = usuario;
    }

    public async Task IntentarAsync(string contrasena)
    {
        Error = null;
        if (_usuario.UsuarioId is not { } id) { Desbloqueado?.Invoke(); return; }
        using var scope = _scopeFactory.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<AutenticacionService>()
            .ValidarDesbloqueoAsync(id, UsarPin ? Pin : null, UsarPin ? null : Login, UsarPin ? null : contrasena);
        if (r.EsFallo) { Error = r.Error; Pin = string.Empty; return; }
        Desbloqueado?.Invoke();
    }
}
