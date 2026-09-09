using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Historial de cambios de precio de venta de una variante.</summary>
public partial class HistorialPrecioViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _titulo = "Historial de precios";
    [ObservableProperty] private string? _subtitulo;

    public ObservableCollection<HistorialPrecioDto> Cambios { get; } = new();

    public HistorialPrecioViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(VarianteInventarioDto variante)
    {
        Titulo = $"Historial de precios · {variante.Producto}";
        var partes = new[] { variante.Sku, variante.Talla, variante.Color }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        Subtitulo = string.Join(" · ", partes);
        _ = CargarAsync(variante.VarianteId);
    }

    private async Task CargarAsync(Guid varianteId)
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<HistorialPrecioService>();
        var lista = await servicio.ListarPorVarianteAsync(varianteId);
        Cambios.Clear();
        foreach (var c in lista) Cambios.Add(c);
    }
}
