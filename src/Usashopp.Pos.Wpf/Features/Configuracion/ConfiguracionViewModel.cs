using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Configuracion;

public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private string _nombreTienda = string.Empty;
    [ObservableProperty] private string? _direccion;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _rfc;
    [ObservableProperty] private string? _mensajePieTicket;
    [ObservableProperty] private decimal _tasaImpuesto;
    [ObservableProperty] private bool _impuestoIncluido;
    [ObservableProperty] private bool _permitirStockNegativo;

    public ConfiguracionViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();
        var c = await servicio.ObtenerAsync();
        NombreTienda = c.NombreTienda;
        Direccion = c.Direccion;
        Telefono = c.Telefono;
        Rfc = c.Rfc;
        MensajePieTicket = c.MensajePieTicket;
        TasaImpuesto = c.TasaImpuesto;
        ImpuestoIncluido = c.ImpuestoIncluidoEnPrecio;
        PermitirStockNegativo = c.PermitirVentaStockNegativo;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        var dto = new ConfiguracionDto(NombreTienda, Direccion, Telefono, Rfc, MensajePieTicket,
            TasaImpuesto, ImpuestoIncluido, PermitirStockNegativo);

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();
        var r = await servicio.GuardarAsync(dto);
        _dialogos.Mensaje(r.Exito ? "Configuración guardada." : r.Error!);
    }

    [RelayCommand]
    private async Task CrearRespaldoAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
            var ruta = await backup.CrearRespaldoAsync();
            _dialogos.Mensaje($"Respaldo creado en:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo crear el respaldo: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RestaurarRespaldo()
    {
        var archivo = _dialogos.SeleccionarArchivoRespaldo();
        if (string.IsNullOrWhiteSpace(archivo)) return;

        if (!_dialogos.Confirmar(
            "Se reemplazará TODA la base de datos actual con el respaldo seleccionado y la " +
            "aplicación se reiniciará. Esta acción no se puede deshacer.\n\n¿Continuar?",
            "Restaurar respaldo"))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
            backup.ProgramarRestauracion(archivo);
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo programar la restauración: {ex.Message}");
            return;
        }

        _dialogos.ReiniciarAplicacion();
    }
}
