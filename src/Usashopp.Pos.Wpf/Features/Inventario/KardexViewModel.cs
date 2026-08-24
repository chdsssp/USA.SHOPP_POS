using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Inventario;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Kardex de una variante: historial de movimientos de inventario con su saldo.</summary>
public partial class KardexViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _titulo = "Kardex";
    [ObservableProperty] private string _subtitulo = string.Empty;
    [ObservableProperty] private int _stockActual;
    [ObservableProperty] private bool _cargando;

    public ObservableCollection<MovimientoKardexDto> Movimientos { get; } = new();

    public KardexViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(VarianteInventarioDto variante)
    {
        Titulo = variante.Producto;
        var partes = new[] { variante.Talla, variante.Color, variante.Sku }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        Subtitulo = string.Join(" · ", partes);
        StockActual = variante.Stock;
        _ = CargarAsync(variante.VarianteId);
    }

    private async Task CargarAsync(Guid varianteId)
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<InventarioService>();
            var lista = await servicio.ObtenerKardexAsync(varianteId);
            Movimientos.Clear();
            foreach (var m in lista) Movimientos.Add(m);
        }
        finally { Cargando = false; }
    }
}
