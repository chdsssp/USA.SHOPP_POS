using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Configuracion;

/// <summary>Asistente de primera configuración: datos de la tienda, folios y (opcional) contraseña.</summary>
public partial class AsistenteInicialViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUser _usuario;

    [ObservableProperty] private string _nombreTienda = string.Empty;
    [ObservableProperty] private string? _direccion;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _rfc;
    [ObservableProperty] private string? _mensajePie;
    [ObservableProperty] private decimal _tasaImpuesto = 0.16m;
    [ObservableProperty] private string _prefijoVenta = "V-";
    [ObservableProperty] private string _prefijoApartado = "A-";
    [ObservableProperty] private string _prefijoCompra = "C-";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public event Action<bool>? Cerrar;

    public AsistenteInicialViewModel(IServiceScopeFactory scopeFactory, ICurrentUser usuario)
    {
        _scopeFactory = scopeFactory;
        _usuario = usuario;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var c = await scope.ServiceProvider.GetRequiredService<ConfiguracionService>().ObtenerAsync();
        NombreTienda = c.NombreTienda;
        Direccion = c.Direccion;
        Telefono = c.Telefono;
        Rfc = c.Rfc;
        MensajePie = c.MensajePieTicket;
        TasaImpuesto = c.TasaImpuesto;
    }

    /// <summary>Las contraseñas viven en los PasswordBox (no se bindean); llegan por parámetro.</summary>
    public async Task GuardarAsync(string actual, string nueva, string confirmar)
    {
        if (Ocupado) return;
        Error = null;

        if (string.IsNullOrWhiteSpace(NombreTienda)) { Error = "El nombre de la tienda es obligatorio."; return; }

        var cambiaContrasena = !string.IsNullOrWhiteSpace(nueva) || !string.IsNullOrWhiteSpace(confirmar) || !string.IsNullOrWhiteSpace(actual);
        if (cambiaContrasena && nueva != confirmar) { Error = "La confirmación no coincide con la nueva contraseña."; return; }

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();

            if (cambiaContrasena && _usuario.UsuarioId is { } uid)
            {
                var usuarios = scope.ServiceProvider.GetRequiredService<UsuarioService>();
                var rp = await usuarios.CambiarMiContrasenaAsync(uid, actual, nueva);
                if (rp.EsFallo) { Error = rp.Error; return; }
            }

            var config = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();
            var r = await config.CompletarAsistenteAsync(new AsistenteConfiguracionDto(
                NombreTienda, Direccion, Telefono, Rfc, MensajePie, TasaImpuesto,
                PrefijoVenta, PrefijoApartado, PrefijoCompra));
            if (r.EsFallo) { Error = r.Error; return; }

            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    public void Omitir() => Cerrar?.Invoke(false);
}
