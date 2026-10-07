using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Application.Proveedores;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Alta de una compra a proveedor con sus líneas.</summary>
public partial class CompraEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private ProveedorDto? _proveedorSeleccionado;
    [ObservableProperty] private ProductoBusquedaDto? _varianteSeleccionada;
    [ObservableProperty] private int _cantidad = 1;
    [ObservableProperty] private decimal _costo;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _guardando;

    /// <summary>Si true, se crea solo la orden (sin ingresar stock); se recibe después.</summary>
    [ObservableProperty] private bool _soloOrdenar;

    public ObservableCollection<ProveedorDto> Proveedores { get; } = new();
    public ObservableCollection<LineaCompraEditable> Lineas { get; } = new();

    public decimal Total => Lineas.Sum(l => l.Importe);

    /// <summary>Texto del botón de producto: el elegido o una invitación a buscar.</summary>
    public string ProductoSeleccionadoTexto => VarianteSeleccionada?.Descripcion ?? "Buscar producto…";

    private readonly IDialogService _dialogos;

    public event Action<bool>? Cerrar;

    public CompraEditorViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    partial void OnVarianteSeleccionadaChanged(ProductoBusquedaDto? value) =>
        OnPropertyChanged(nameof(ProductoSeleccionadoTexto));

    [RelayCommand]
    private void BuscarProducto()
    {
        var producto = _dialogos.SeleccionarProducto();
        if (producto is not null) VarianteSeleccionada = producto;
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var proveedores = await scope.ServiceProvider.GetRequiredService<ProveedorService>().ListarAsync();

        Proveedores.Clear();
        foreach (var p in proveedores) Proveedores.Add(p);
        ProveedorSeleccionado = Proveedores.FirstOrDefault();
    }

    [RelayCommand]
    private void AgregarLinea()
    {
        Error = null;
        if (VarianteSeleccionada is null) { Error = "Selecciona un producto."; return; }
        if (Cantidad <= 0) { Error = "La cantidad debe ser mayor que cero."; return; }

        Lineas.Add(new LineaCompraEditable
        {
            VarianteId = VarianteSeleccionada.VarianteId,
            Descripcion = VarianteSeleccionada.Descripcion,
            Cantidad = Cantidad,
            Costo = Costo
        });
        OnPropertyChanged(nameof(Total));
        Cantidad = 1;
        Costo = 0;
        VarianteSeleccionada = null;
    }

    [RelayCommand]
    private void QuitarLinea(LineaCompraEditable linea)
    {
        Lineas.Remove(linea);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        if (ProveedorSeleccionado is null) { Error = "Selecciona un proveedor."; return; }
        if (Lineas.Count == 0) { Error = "Agrega al menos una línea."; return; }

        var dto = new NuevaCompraDto(
            ProveedorSeleccionado.Id,
            Lineas.Select(l => new NuevaLineaCompraDto(l.VarianteId, l.Cantidad, l.Costo)).ToList());

        Guardando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            if (SoloOrdenar)
            {
                var orden = scope.ServiceProvider.GetRequiredService<OrdenCompraService>();
                var r = await orden.CrearOrdenAsync(dto);
                if (r.EsFallo) { Error = r.Error; return; }
            }
            else
            {
                var servicio = scope.ServiceProvider.GetRequiredService<RegistrarCompraService>();
                var r = await servicio.EjecutarAsync(dto);
                if (r.EsFallo) { Error = r.Error; return; }
            }
            Cerrar?.Invoke(true);
        }
        finally { Guardando = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
