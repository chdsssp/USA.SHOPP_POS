using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Toma de inventario físico: captura el conteo real y aplica los ajustes.</summary>
public partial class TomaFisicaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _busqueda = string.Empty;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<TomaFisicaEditable> Lineas { get; } = new();

    public event Action<bool>? Cerrar;

    public TomaFisicaViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
        var lista = await servicio.ListarAsync(string.IsNullOrWhiteSpace(Busqueda) ? null : Busqueda);
        Lineas.Clear();
        foreach (var v in lista)
            Lineas.Add(new TomaFisicaEditable
            {
                VarianteId = v.VarianteId,
                Sku = v.Sku,
                Descripcion = $"{v.Producto}{(string.IsNullOrWhiteSpace(v.Talla) && string.IsNullOrWhiteSpace(v.Color) ? "" : $" · {v.Talla} {v.Color}".TrimEnd())}",
                StockSistema = v.Stock,
                Conteo = v.Stock // prefill: sin cambios por defecto
            });
    }

    [RelayCommand]
    private async Task AplicarAsync()
    {
        if (Ocupado) return;
        Error = null;

        var cambios = Lineas
            .Where(l => l.Conteo != l.StockSistema)
            .Select(l => new TomaFisicaLineaDto(l.VarianteId, l.Conteo))
            .ToList();

        if (cambios.Count == 0) { Error = "No hay diferencias que aplicar."; return; }

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
            var r = await servicio.AplicarTomaFisicaAsync(cambios);
            if (r.EsFallo) { Error = r.Error; return; }
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
