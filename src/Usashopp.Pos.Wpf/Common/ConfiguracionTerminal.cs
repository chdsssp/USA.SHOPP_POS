using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Serilog;
using Usashopp.Pos.Application.Common.Interfaces.System;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>Cuándo mostrar los teclados en pantalla propios de la app.</summary>
public enum ModoTeclado
{
    /// <summary>Solo cuando el campo se toca con el dedo/lápiz (con mouse no aparece).</summary>
    SoloTactil = 0,
    Siempre = 1,
    Nunca = 2
}

/// <summary>
/// Preferencias de ESTE equipo (no de la tienda): tamaño de la interfaz, teclado en pantalla y
/// modo de inicio. Se guardan en <c>terminal.json</c> junto a los datos de la app, no en la BD,
/// porque cada caja puede tener otra pantalla. Lectura/escritura tolerante a fallos.
/// </summary>
public partial class ConfiguracionTerminal : ObservableObject, IPreferenciasRespaldo
{
    private readonly string _ruta;

    /// <summary>Factor de escala de toda la UI (1.0 = 100 %).</summary>
    [ObservableProperty] private double _escalaUi = 1.0;
    [ObservableProperty] private ModoTeclado _teclado = ModoTeclado.SoloTactil;
    /// <summary>Entrar directo al POS a pantalla completa (modo cajero) al iniciar sesión.</summary>
    [ObservableProperty] private bool _pantallaCompletaAlIniciar;
    /// <summary>Carpeta de respaldo externo (USB/disco). Vacío = desactivado.</summary>
    [ObservableProperty] private string? _carpetaRespaldoExterno;
    /// <summary>Carpeta (USB o red local) donde se dejan los instaladores nuevos. Vacío = no se revisa.</summary>
    [ObservableProperty] private string? _carpetaActualizaciones;

    public static readonly double[] EscalasDisponibles = { 1.0, 1.15, 1.25, 1.4 };

    public ConfiguracionTerminal(IConfiguration config)
    {
        var carpeta = config["Infrastructure:CarpetaTerminal"];
        if (string.IsNullOrWhiteSpace(carpeta))
            carpeta = "%ProgramData%/USASHOPP POS";
        carpeta = Environment.ExpandEnvironmentVariables(carpeta).Replace('/', Path.DirectorySeparatorChar);
        _ruta = Path.Combine(carpeta, "terminal.json");
        Cargar();
        // Cada cambio se guarda al momento (son preferencias del equipo, sin botón "Guardar").
        PropertyChanged += (_, _) => Guardar();
    }

    private sealed record Datos(double EscalaUi, ModoTeclado Teclado, bool PantallaCompletaAlIniciar, string? CarpetaRespaldoExterno,
        string? CarpetaActualizaciones = null);

    private void Cargar()
    {
        try
        {
            if (!File.Exists(_ruta)) return;
            var d = JsonSerializer.Deserialize<Datos>(File.ReadAllText(_ruta));
            if (d is null) return;
            EscalaUi = d.EscalaUi is >= 0.8 and <= 2 ? d.EscalaUi : 1.0;
            Teclado = d.Teclado;
            PantallaCompletaAlIniciar = d.PantallaCompletaAlIniciar;
            CarpetaRespaldoExterno = d.CarpetaRespaldoExterno;
            CarpetaActualizaciones = d.CarpetaActualizaciones;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo leer {Ruta}; se usan valores por defecto", _ruta);
        }
    }

    public void Guardar()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
            File.WriteAllText(_ruta, JsonSerializer.Serialize(
                new Datos(EscalaUi, Teclado, PantallaCompletaAlIniciar, CarpetaRespaldoExterno, CarpetaActualizaciones),
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo guardar {Ruta}", _ruta);
        }
    }
}
