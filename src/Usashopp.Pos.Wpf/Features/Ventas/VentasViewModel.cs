using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Ventas;

/// <summary>Historial de ventas: filtro por fechas, detalle y reimpresión.</summary>
public partial class VentasViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private DateTime _desde = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _hasta = DateTime.Today.AddDays(1);
    [ObservableProperty] private bool _cargando;
    [ObservableProperty] private VentaResumenDto? _seleccionada;
    [ObservableProperty] private VentaDetalleDto? _detalle;

    /// <summary>Si el usuario puede cancelar ventas (permiso ventas.cancelar).</summary>
    public bool PuedeCancelar { get; }

    public ObservableCollection<VentaResumenDto> Ventas { get; } = new();

    public VentasViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos, ICurrentUser currentUser)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        PuedeCancelar = currentUser.TienePermiso(Permisos.VentasCancelar);
        _ = CargarAsync();
    }

    partial void OnSeleccionadaChanged(VentaResumenDto? value) => _ = CargarDetalleAsync();

    [RelayCommand]
    private async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ConsultarVentasService>();
            var lista = await servicio.ListarAsync(Desde, Hasta.AddDays(1).AddTicks(-1));

            Ventas.Clear();
            foreach (var v in lista) Ventas.Add(v);
            Detalle = null;
        }
        finally { Cargando = false; }
    }

    private async Task CargarDetalleAsync()
    {
        if (Seleccionada is null) { Detalle = null; return; }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarVentasService>();
        Detalle = await servicio.ObtenerDetalleAsync(Seleccionada.Id);
    }

    [RelayCommand]
    private void VistaPrevia()
    {
        if (Seleccionada is null)
        {
            _dialogos.Mensaje("Selecciona una venta para ver la vista previa de su ticket.");
            return;
        }
        _dialogos.MostrarVistaPreviaTicket(Seleccionada.Id);
    }

    [RelayCommand]
    private async Task ReimprimirAsync()
    {
        if (Seleccionada is null)
        {
            _dialogos.Mensaje("Selecciona una venta para reimprimir su ticket.");
            return;
        }
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarVentasService>();
        var r = await servicio.ReimprimirAsync(Seleccionada.Id);
        _dialogos.Mensaje(r.Exito ? $"Ticket de {Seleccionada.Folio} enviado a impresión." : r.Error!);
    }

    [RelayCommand]
    private async Task CancelarVentaAsync()
    {
        if (Seleccionada is null)
        {
            _dialogos.Mensaje("Selecciona una venta para cancelar.");
            return;
        }
        if (!_dialogos.Confirmar($"¿Cancelar la venta {Seleccionada.Folio}? Se reintegrará el stock.", "Cancelar venta"))
            return;
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CancelarVentaService>();
        var r = await servicio.EjecutarAsync(Seleccionada.Id);
        _dialogos.Mensaje(r.Exito ? "Venta cancelada; se reintegró el stock." : r.Error!);
        if (r.Exito) await CargarAsync();
    }
}
