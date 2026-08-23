using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Ajuste manual de existencias de una variante.</summary>
public partial class AjusteStockViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _varianteId;

    [ObservableProperty] private string _descripcion = string.Empty;
    [ObservableProperty] private int _stockActual;
    [ObservableProperty] private int _nuevaCantidad;
    [ObservableProperty] private string? _motivo;
    [ObservableProperty] private string? _error;

    public event Action<bool>? Cerrar;

    public AjusteStockViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(VarianteInventarioDto variante)
    {
        _varianteId = variante.VarianteId;
        Descripcion = $"{variante.Producto} — {variante.Talla} {variante.Color}".Trim();
        StockActual = variante.Stock;
        NuevaCantidad = variante.Stock;
    }

    [RelayCommand]
    private void Incrementar() => NuevaCantidad++;

    [RelayCommand]
    private void Decrementar()
    {
        if (NuevaCantidad > 0) NuevaCantidad--;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        if (NuevaCantidad < 0)
        {
            Error = "La cantidad no puede ser negativa.";
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
        var resultado = await servicio.AjustarStockAsync(new AjusteStockDto(_varianteId, NuevaCantidad, Motivo));

        if (resultado.EsFallo)
        {
            Error = resultado.Error;
            return;
        }

        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
