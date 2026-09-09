using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class ComprasViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private CompraResumenDto? _seleccionada;
    [ObservableProperty] private CompraDetalleDto? _detalle;

    public ObservableCollection<CompraResumenDto> Compras { get; } = new();

    public ComprasViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    partial void OnSeleccionadaChanged(CompraResumenDto? value) => _ = CargarDetalleAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarComprasService>();
        var lista = await servicio.ListarAsync();
        Compras.Clear();
        foreach (var c in lista) Compras.Add(c);
        Detalle = null;
    }

    private async Task CargarDetalleAsync()
    {
        if (Seleccionada is null) { Detalle = null; return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarComprasService>();
        Detalle = await servicio.ObtenerDetalleAsync(Seleccionada.Id);
    }

    [RelayCommand]
    private async Task NuevaCompraAsync()
    {
        if (_dialogos.MostrarEditorCompra()) await CargarAsync();
    }

    [RelayCommand]
    private async Task RecibirAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una compra para recibir."); return; }
        if (Seleccionada.Estado is "Recibida" or "Cancelada")
        {
            _dialogos.Mensaje($"La compra {Seleccionada.Folio} está {Seleccionada.Estado.ToLower()}; no hay nada por recibir.");
            return;
        }
        if (_dialogos.MostrarRecepcionCompra(Seleccionada.Id)) await CargarAsync();
    }

    [RelayCommand]
    private async Task DevolverAProveedorAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una compra para devolver."); return; }
        if (Seleccionada.Estado is "Ordenada" or "Cancelada")
        {
            _dialogos.Mensaje("Solo puedes devolver mercancía ya recibida.");
            return;
        }
        if (_dialogos.MostrarDevolucionProveedor(Seleccionada.Id)) await CargarAsync();
    }

    [RelayCommand]
    private async Task PagosAsync()
    {
        if (Seleccionada is null) { _dialogos.Mensaje("Selecciona una compra para ver sus cuentas por pagar."); return; }
        if (_dialogos.MostrarPagosCompra(Seleccionada.Id)) await CargarAsync();
    }
}
