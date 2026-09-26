using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Buscador de productos reutilizable (estilo POS): lista todos los productos y filtra por
/// texto (nombre, marca, talla, color). Devuelve la variante elegida.
/// </summary>
public partial class SelectorProductoViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _busqueda = "";
    [ObservableProperty] private ProductoBusquedaDto? _seleccionado;
    [ObservableProperty] private bool _cargando;

    public ObservableCollection<ProductoBusquedaDto> Resultados { get; } = new();

    public event Action<bool>? Cerrar;

    public SelectorProductoViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = BuscarAsync();
    }

    partial void OnBusquedaChanged(string value) => _ = BuscarAsync();

    private async Task BuscarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<BuscarProductosService>();
            var texto = Busqueda?.Trim() ?? "";
            var lista = texto.Length == 0
                ? await servicio.ParaGridAsync(null)
                : await servicio.PorTextoAsync(texto);

            Resultados.Clear();
            foreach (var p in lista) Resultados.Add(p);
        }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private void Elegir(ProductoBusquedaDto? producto)
    {
        var elegido = producto ?? Seleccionado;
        if (elegido is null) return;
        Seleccionado = elegido;
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
