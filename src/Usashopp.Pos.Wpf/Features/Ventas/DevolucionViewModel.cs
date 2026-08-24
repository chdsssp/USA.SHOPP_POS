using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Ventas;

/// <summary>Devolución parcial/total de mercancía de una venta.</summary>
public partial class DevolucionViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _ventaId;

    [ObservableProperty] private string _titulo = "Devolver mercancía";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _cargando;

    public ObservableCollection<LineaDevolucionEditable> Lineas { get; } = new();

    public event Action<bool>? Cerrar;

    public DevolucionViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid ventaId, string folio)
    {
        _ventaId = ventaId;
        Titulo = $"Devolver mercancía · {folio}";
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
            var lista = await servicio.ObtenerLineasAsync(_ventaId);
            Lineas.Clear();
            foreach (var l in lista)
                Lineas.Add(new LineaDevolucionEditable
                {
                    VarianteId = l.VarianteId,
                    Descripcion = l.Descripcion,
                    PrecioUnitario = l.PrecioUnitario,
                    Vendida = l.Vendida,
                    Devuelta = l.Devuelta,
                    Disponible = l.Disponible
                });
        }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        Error = null;
        var items = Lineas
            .Where(l => l.ADevolver > 0)
            .Select(l => new DevolucionItemDto(l.VarianteId, l.ADevolver))
            .ToList();

        if (items.Count == 0) { Error = "Indica al menos una cantidad a devolver."; return; }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
        var r = await servicio.EjecutarAsync(_ventaId, items);
        if (r.EsFallo) { Error = r.Error; return; }

        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
