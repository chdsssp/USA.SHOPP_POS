using System.Collections.ObjectModel;
using System.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.Hardware;
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
    [ObservableProperty] private string? _logoRuta;
    [ObservableProperty] private string? _logoAbsoluto;
    [ObservableProperty] private string? _impresoraTicket;
    [ObservableProperty] private string? _impresoraEtiquetas;

    /// <summary>Impresoras de Windows disponibles para elegir la del ticket / etiquetas.</summary>
    public ObservableCollection<string> Impresoras { get; } = new();

    public bool TieneLogo => !string.IsNullOrWhiteSpace(LogoAbsoluto);
    partial void OnLogoAbsolutoChanged(string? value) => OnPropertyChanged(nameof(TieneLogo));

    /// <summary>Preferencias de este equipo (tamaño de UI, teclado, respaldo externo).</summary>
    public ConfiguracionTerminal Terminal { get; }

    public ConfiguracionViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos, ConfiguracionTerminal terminal)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        Terminal = terminal;
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
        LogoRuta = c.LogoRuta;
        LogoAbsoluto = scope.ServiceProvider.GetRequiredService<IAlmacenImagenes>().ObtenerRutaCompleta(LogoRuta);

        CargarImpresoras();
        CargarRespaldos();
        ImpresoraTicket = c.ImpresoraTicket;
        ImpresoraEtiquetas = c.ImpresoraEtiquetas;
    }

    /// <summary>Enumera las impresoras instaladas en Windows (sin romper si el spooler falla).</summary>
    private void CargarImpresoras()
    {
        Impresoras.Clear();
        try
        {
            using var server = new LocalPrintServer();
            foreach (var nombre in server.GetPrintQueues().Select(q => q.Name).OrderBy(n => n))
                Impresoras.Add(nombre);
        }
        catch { /* sin spooler o sin impresoras: la lista queda vacía */ }
    }

    [RelayCommand]
    private async Task ElegirLogoAsync()
    {
        var ruta = _dialogos.SeleccionarImagen();
        if (string.IsNullOrWhiteSpace(ruta)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var almacen = scope.ServiceProvider.GetRequiredService<IAlmacenImagenes>();
            LogoRuta = await almacen.GuardarAsync(ruta);
            LogoAbsoluto = almacen.ObtenerRutaCompleta(LogoRuta);
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo cargar el logo: {ex.Message}");
        }
    }

    [RelayCommand]
    private void QuitarLogo()
    {
        LogoRuta = null;
        LogoAbsoluto = null;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        var dto = new ConfiguracionDto(NombreTienda, Direccion, Telefono, Rfc, MensajePieTicket,
            TasaImpuesto, ImpuestoIncluido, PermitirStockNegativo, LogoRuta, ImpresoraTicket, ImpresoraEtiquetas);

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();
        var r = await servicio.GuardarAsync(dto);
        if (r.Exito)
            WeakReferenceMessenger.Default.Send(new ConfiguracionCambiadaMessage());
        _dialogos.Mensaje(r.Exito ? "Configuración guardada." : r.Error!);
    }

    [RelayCommand]
    private async Task ImprimirPruebaAsync()
    {
        if (string.IsNullOrWhiteSpace(ImpresoraTicket))
        {
            _dialogos.Mensaje("Selecciona una impresora antes de imprimir la prueba.");
            return;
        }

        // Guarda primero para que la prueba use la impresora seleccionada.
        var dto = new ConfiguracionDto(NombreTienda, Direccion, Telefono, Rfc, MensajePieTicket,
            TasaImpuesto, ImpuestoIncluido, PermitirStockNegativo, LogoRuta, ImpresoraTicket, ImpresoraEtiquetas);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ConfiguracionService>().GuardarAsync(dto);
            WeakReferenceMessenger.Default.Send(new ConfiguracionCambiadaMessage());
            await scope.ServiceProvider.GetRequiredService<ITicketPrinter>().ImprimirPruebaAsync();
            _dialogos.Mensaje($"Se envió una impresión de prueba a «{ImpresoraTicket}».");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo imprimir: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CrearRespaldoAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
            var ruta = await backup.CrearRespaldoAsync();
            var ok = backup.VerificarRespaldo(ruta);
            CargarRespaldos();
            _dialogos.Mensaje(ok
                ? $"Respaldo creado y verificado:\n{ruta}"
                : $"Se creó el respaldo pero NO pasó la verificación de integridad:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo crear el respaldo: {ex.Message}");
        }
    }

    // ---------- Lista de respaldos y destino externo ----------

    public ObservableCollection<RespaldoInfo> Respaldos { get; } = new();
    [ObservableProperty] private RespaldoInfo? _respaldoSeleccionado;
    [ObservableProperty] private string _estadoExterno = string.Empty;

    private void CargarRespaldos()
    {
        using var scope = _scopeFactory.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        Respaldos.Clear();
        foreach (var r in backup.ListarRespaldos()) Respaldos.Add(r);

        var ext = backup.ObtenerEstadoExterno();
        EstadoExterno = !ext.Configurado ? "Sin carpeta externa: los respaldos solo están en este equipo."
            : !ext.Disponible ? $"La carpeta externa «{Terminal.CarpetaRespaldoExterno}» no está conectada."
            : ext.Ultimo is { } u ? $"Último respaldo externo: {u:dd/MM/yyyy HH:mm}"
            : "Carpeta externa conectada; aún sin respaldos ahí.";
    }

    [RelayCommand]
    private void ElegirCarpetaExterna()
    {
        var carpeta = _dialogos.SeleccionarCarpeta("Carpeta para respaldos externos (USB o disco)");
        if (string.IsNullOrWhiteSpace(carpeta)) return;
        Terminal.CarpetaRespaldoExterno = carpeta;
        CargarRespaldos();
    }

    [RelayCommand]
    private void QuitarCarpetaExterna()
    {
        Terminal.CarpetaRespaldoExterno = null;
        CargarRespaldos();
    }

    [RelayCommand]
    private void RestaurarSeleccionado()
    {
        if (RespaldoSeleccionado is not { } r)
        {
            _dialogos.Mensaje("Selecciona un respaldo de la lista.");
            return;
        }
        if (r.Verificado == false &&
            !_dialogos.Confirmar("Este respaldo NO pasó la verificación de integridad. ¿Restaurarlo de todos modos?", "Respaldo dañado"))
            return;
        ProgramarYReiniciar(r.Ruta);
    }

    [RelayCommand]
    private void RestaurarRespaldo()
    {
        var archivo = _dialogos.SeleccionarArchivoRespaldo();
        if (string.IsNullOrWhiteSpace(archivo)) return;
        ProgramarYReiniciar(archivo);
    }

    private void ProgramarYReiniciar(string archivo)
    {
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
