using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Devolución de mercancía recibida a un proveedor.</summary>
public partial class DevolucionProveedorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _compraId;

    [ObservableProperty] private string _titulo = "Devolver a proveedor";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<DevolucionProveedorEditable> Lineas { get; } = new();

    public event Action<bool>? Cerrar;

    public DevolucionProveedorViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid compraId)
    {
        _compraId = compraId;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarComprasService>();
        var compra = await servicio.ObtenerDetalleAsync(_compraId);
        if (compra is null) { Error = "La compra no existe."; return; }

        Titulo = $"Devolver a proveedor · {compra.Folio}";
        Lineas.Clear();
        // Agrupa por variante (una variante podría estar en varias líneas).
        foreach (var g in compra.Lineas.Where(l => l.CantidadRecibida > 0).GroupBy(l => l.VarianteId))
            Lineas.Add(new DevolucionProveedorEditable
            {
                VarianteId = g.Key,
                Descripcion = g.First().Descripcion,
                Recibido = g.Sum(l => l.CantidadRecibida),
                ADevolver = 0
            });
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (Ocupado) return;
        Error = null;

        var lineas = Lineas
            .Where(l => l.ADevolver > 0)
            .Select(l => new DevolucionProveedorLineaDto(l.VarianteId, l.ADevolver))
            .ToList();
        if (lineas.Count == 0) { Error = "Indica al menos una cantidad a devolver."; return; }

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<DevolucionProveedorService>();
            var r = await servicio.EjecutarAsync(new DevolucionProveedorDto(_compraId, lineas));
            if (r.EsFallo) { Error = r.Error; return; }
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
