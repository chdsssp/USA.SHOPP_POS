using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Alta y edición de un producto con sus variantes.</summary>
public partial class ProductoEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _id;

    [ObservableProperty] private string _titulo = "Nuevo producto";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _marca;
    [ObservableProperty] private string? _descripcion;
    [ObservableProperty] private CategoriaDto? _categoriaSeleccionada;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _guardando;

    public ObservableCollection<CategoriaDto> Categorias { get; } = new();
    public ObservableCollection<VarianteEditable> Variantes { get; } = new();

    public event Action<bool>? Cerrar;

    public ProductoEditorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>Inicializa en modo alta (null) o edición (id del producto).</summary>
    public async void Inicializar(Guid? productoId)
    {
        _id = productoId;
        Titulo = productoId is null ? "Nuevo producto" : "Editar producto";

        using var scope = _scopeFactory.CreateScope();
        var categoriasSvc = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var lista = await categoriasSvc.ListarAsync();
        Categorias.Clear();
        foreach (var c in lista) Categorias.Add(c);

        if (productoId is null)
        {
            Variantes.Add(new VarianteEditable());
            CategoriaSeleccionada = Categorias.FirstOrDefault();
            return;
        }

        var producto = scope.ServiceProvider.GetRequiredService<ProductoService>();
        var dto = await producto.ObtenerParaEdicionAsync(productoId.Value);
        if (dto is null) { CategoriaSeleccionada = Categorias.FirstOrDefault(); return; }

        Nombre = dto.Nombre;
        Marca = dto.Marca;
        Descripcion = dto.Descripcion;
        CategoriaSeleccionada = Categorias.FirstOrDefault(c => c.Id == dto.CategoriaId) ?? Categorias.FirstOrDefault();

        Variantes.Clear();
        foreach (var v in dto.Variantes)
            Variantes.Add(new VarianteEditable
            {
                Id = v.Id,
                Sku = v.Sku,
                CodigoBarras = v.CodigoBarras,
                Talla = v.Talla,
                Color = v.Color,
                Precio = v.Precio,
                Costo = v.Costo,
                StockInicial = v.StockInicial,
                StockMinimo = v.StockMinimo
            });
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

        if (string.IsNullOrWhiteSpace(Nombre)) { Error = "El nombre del producto es obligatorio."; return; }
        if (CategoriaSeleccionada is null) { Error = "Selecciona una categoría."; return; }
        if (Variantes.Any(v => string.IsNullOrWhiteSpace(v.Sku))) { Error = "Cada variante necesita un SKU."; return; }

        var dto = new NuevoProductoDto(
            Nombre.Trim(),
            CategoriaSeleccionada.Id,
            Variantes.Select(v => new VarianteEntradaDto(
                v.Sku, v.CodigoBarras, v.Talla, v.Color,
                v.Precio, v.Costo, v.StockInicial, v.StockMinimo, v.Id)).ToList(),
            Descripcion,
            Marca);

        Guardando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ProductoService>();

            if (_id is null)
            {
                var r = await servicio.CrearAsync(dto);
                if (r.EsFallo) { Error = r.Error; return; }
            }
            else
            {
                var r = await servicio.ActualizarAsync(_id.Value, dto);
                if (r.EsFallo) { Error = r.Error; return; }
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
