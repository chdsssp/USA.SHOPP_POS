using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Alta de un producto con una o más variantes.</summary>
public partial class ProductoEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _marca;
    [ObservableProperty] private string? _descripcion;
    [ObservableProperty] private CategoriaDto? _categoriaSeleccionada;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _guardando;

    public ObservableCollection<CategoriaDto> Categorias { get; } = new();
    public ObservableCollection<VarianteEditable> Variantes { get; } = new();

    /// <summary>Solicita cerrar la ventana; el parámetro indica si se guardó.</summary>
    public event Action<bool>? Cerrar;

    public ProductoEditorViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Variantes.Add(new VarianteEditable());
        _ = CargarCategoriasAsync();
    }

    private async Task CargarCategoriasAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var lista = await servicio.ListarAsync();

        Categorias.Clear();
        foreach (var c in lista)
            Categorias.Add(c);
        CategoriaSeleccionada = Categorias.FirstOrDefault();
    }

    [RelayCommand]
    private void AgregarVariante() => Variantes.Add(new VarianteEditable());

    [RelayCommand]
    private void QuitarVariante(VarianteEditable variante)
    {
        if (Variantes.Count > 1)
            Variantes.Remove(variante);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            Error = "El nombre del producto es obligatorio.";
            return;
        }
        if (CategoriaSeleccionada is null)
        {
            Error = "Selecciona una categoría.";
            return;
        }
        if (Variantes.Any(v => string.IsNullOrWhiteSpace(v.Sku)))
        {
            Error = "Cada variante necesita un SKU.";
            return;
        }

        var dto = new NuevoProductoDto(
            Nombre.Trim(),
            CategoriaSeleccionada.Id,
            Variantes.Select(v => new VarianteEntradaDto(
                v.Sku, v.CodigoBarras, v.Talla, v.Color,
                v.Precio, v.Costo, v.StockInicial, v.StockMinimo)).ToList(),
            Descripcion,
            Marca);

        Guardando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ProductoService>();
            var resultado = await servicio.CrearAsync(dto);

            if (resultado.EsFallo)
            {
                Error = resultado.Error;
                return;
            }

            Cerrar?.Invoke(true);
        }
        finally
        {
            Guardando = false;
        }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
