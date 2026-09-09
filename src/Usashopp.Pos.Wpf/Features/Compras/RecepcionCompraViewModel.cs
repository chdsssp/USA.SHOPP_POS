using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Recepción (total o parcial) de una orden de compra.</summary>
public partial class RecepcionCompraViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _compraId;

    [ObservableProperty] private string _titulo = "Recibir mercancía";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<RecepcionLineaEditable> Lineas { get; } = new();

    public event Action<bool>? Cerrar;

    public RecepcionCompraViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

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

        Titulo = $"Recibir mercancía · {compra.Folio}";
        Lineas.Clear();
        foreach (var l in compra.Lineas.Where(l => l.Pendiente > 0))
            Lineas.Add(new RecepcionLineaEditable
            {
                DetalleId = l.DetalleId,
                Descripcion = l.Descripcion,
                Cantidad = l.Cantidad,
                Recibido = l.CantidadRecibida,
                Pendiente = l.Pendiente,
                ARecibir = l.Pendiente // por defecto, recibir todo lo pendiente
            });
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (Ocupado) return;
        Error = null;

        var lineas = Lineas
            .Where(l => l.ARecibir > 0)
            .Select(l => new RecepcionLineaDto(l.DetalleId, l.ARecibir))
            .ToList();
        if (lineas.Count == 0) { Error = "Indica al menos una cantidad a recibir."; return; }

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<OrdenCompraService>();
            var r = await servicio.RecibirAsync(new RecepcionCompraDto(_compraId, lineas));
            if (r.EsFallo) { Error = r.Error; return; }
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
