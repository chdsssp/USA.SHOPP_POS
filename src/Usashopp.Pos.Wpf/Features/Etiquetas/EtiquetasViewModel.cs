using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>
/// Módulo de etiquetas: lista/busca productos de inventario, muestra la vista previa de la etiqueta
/// 4×6" del producto seleccionado y la imprime en la impresora de etiquetas configurada.
/// </summary>
public partial class EtiquetasViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    private string _nombreNegocio = "";
    private string? _logoAbsoluto;

    [ObservableProperty] private string _busqueda = "";
    [ObservableProperty] private VarianteInventarioDto? _seleccionado;
    [ObservableProperty] private EtiquetaDatos? _preview;
    [ObservableProperty] private FrameworkElement? _previewVisual;
    [ObservableProperty] private TamanoEtiqueta _tamanoSeleccionado = TamanoEtiqueta.Todos[0];
    [ObservableProperty] private bool _cargando;

    public ObservableCollection<VarianteInventarioDto> Productos { get; } = new();

    /// <summary>Tamaños de papel/etiqueta disponibles para imprimir.</summary>
    public IReadOnlyList<TamanoEtiqueta> Tamanos { get; } = TamanoEtiqueta.Todos;

    public EtiquetasViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var config = await scope.ServiceProvider.GetRequiredService<ConfiguracionService>().ObtenerAsync();
        _nombreNegocio = config.NombreTienda;
        _logoAbsoluto = scope.ServiceProvider.GetRequiredService<IAlmacenImagenes>().ObtenerRutaCompleta(config.LogoRuta);
        await BuscarAsync();
    }

    partial void OnBusquedaChanged(string value) => _ = BuscarAsync();

    private async Task BuscarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var inv = scope.ServiceProvider.GetRequiredService<InventarioService>();
            var lista = await inv.ListarAsync(string.IsNullOrWhiteSpace(Busqueda) ? null : Busqueda);
            Productos.Clear();
            foreach (var v in lista) Productos.Add(v);
        }
        finally { Cargando = false; }
    }

    partial void OnSeleccionadoChanged(VarianteInventarioDto? value)
    {
        Preview = value is null ? null : new EtiquetaDatos
        {
            LogoAbsoluto = _logoAbsoluto,
            NombreNegocio = _nombreNegocio,
            Talla = value.Talla ?? "",
            Nombre = value.Producto,
            Marca = value.Marca ?? "",
            Color = value.Color ?? "",
            Codigo = value.CodigoBarras ?? ""
        };
        ActualizarVisual();
        ImprimirCommand.NotifyCanExecuteChanged();
    }

    partial void OnTamanoSeleccionadoChanged(TamanoEtiqueta value) => ActualizarVisual();

    /// <summary>Reconstruye la vista previa con el tamaño elegido (una instancia nueva del visual).</summary>
    private void ActualizarVisual() =>
        PreviewVisual = Preview is null ? null : EtiquetaVisualFactory.Construir(Preview, TamanoSeleccionado);

    private bool PuedeImprimir => Seleccionado is not null;

    [RelayCommand(CanExecute = nameof(PuedeImprimir))]
    private async Task ImprimirAsync()
    {
        if (Preview is null) return;
        if (!Preview.TieneCodigo)
        {
            _dialogos.Mensaje("El producto seleccionado no tiene código de barras asignado. " +
                              "Asígnalo en Inventario para poder imprimir su etiqueta.");
            return;
        }

        // Se relee la impresora configurada para usar siempre la última selección.
        string? impresora;
        using (var scope = _scopeFactory.CreateScope())
            impresora = (await scope.ServiceProvider.GetRequiredService<ConfiguracionService>().ObtenerAsync()).ImpresoraEtiquetas;

        if (string.IsNullOrWhiteSpace(impresora))
        {
            _dialogos.Mensaje("No hay una impresora de etiquetas configurada. " +
                              "Ve a Configuración → Impresora de etiquetas.");
            return;
        }

        try
        {
            EtiquetaImpresora.Imprimir(Preview, TamanoSeleccionado, impresora);
            _dialogos.Mensaje($"Etiqueta enviada a «{impresora}» ({TamanoSeleccionado.Nombre}).");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo imprimir la etiqueta: {ex.Message}");
        }
    }
}
