using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>
/// Pantalla de Inventario: lista de variantes con búsqueda, filtro de bajo stock,
/// alta de productos y ajuste de existencias.
/// </summary>
public partial class InventarioViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private string _busqueda = string.Empty;
    [ObservableProperty] private bool _soloBajoStock;
    [ObservableProperty] private bool _cargando;
    [ObservableProperty] private VarianteInventarioDto? _seleccionada;

    public ObservableCollection<VarianteInventarioDto> Items { get; } = new();

    public InventarioViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    partial void OnSoloBajoStockChanged(bool value) => _ = CargarAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
            var lista = await servicio.ListarAsync(Busqueda, SoloBajoStock);

            Items.Clear();
            foreach (var item in lista)
                Items.Add(item);
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task NuevoProductoAsync()
    {
        if (_dialogos.MostrarEditorProducto(null))
            await CargarAsync();
    }

    [RelayCommand]
    private async Task EditarProductoAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una fila para editar su producto."); return; }
        if (_dialogos.MostrarEditorProducto(Seleccionada.ProductoId))
            await CargarAsync();
    }

    [RelayCommand]
    private async Task AjustarStockAsync()
    {
        if (Seleccionada is null)
        {
            _dialogos.Mensaje("Selecciona una variante para ajustar su stock.");
            return;
        }

        if (_dialogos.MostrarAjusteStock(Seleccionada))
            await CargarAsync();
    }

    [RelayCommand]
    private void Kardex()
    {
        if (Seleccionada is null)
        {
            _dialogos.Mensaje("Selecciona una variante para ver su kardex.");
            return;
        }

        _dialogos.MostrarKardex(Seleccionada);
    }

    [RelayCommand]
    private async Task ExportarCsvAsync()
    {
        var ruta = _dialogos.GuardarComoCsv($"catalogo_{DateTime.Now:yyyyMMdd}.csv");
        if (string.IsNullOrWhiteSpace(ruta)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoCsvService>();
            var csv = await servicio.ExportarAsync();
            await File.WriteAllTextAsync(ruta, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            _dialogos.Mensaje($"Catálogo exportado a:\n{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo exportar: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportarCsvAsync()
    {
        var ruta = _dialogos.SeleccionarArchivoCsv();
        if (string.IsNullOrWhiteSpace(ruta)) return;
        if (!_dialogos.Confirmar(
                "Se crearán productos nuevos y se actualizarán los existentes (por SKU) según el archivo. ¿Continuar?",
                "Importar catálogo"))
            return;

        try
        {
            var contenido = await File.ReadAllTextAsync(ruta);
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoCsvService>();
            var r = await servicio.ImportarAsync(contenido);
            if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }

            var res = r.Valor!;
            var resumen = $"Importación terminada.\n\nCreados: {res.Creados}\nActualizados: {res.Actualizados}\nOmitidos: {res.Omitidos}";
            if (res.Errores.Count > 0)
                resumen += "\n\n" + string.Join("\n", res.Errores.Take(10));
            _dialogos.Mensaje(resumen);
            await CargarAsync();
        }
        catch (Exception ex)
        {
            _dialogos.Mensaje($"No se pudo importar: {ex.Message}");
        }
    }
}
