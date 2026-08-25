using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Configuracion;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Ventas;

/// <summary>
/// Vista previa en pantalla del ticket de una venta (mismo contenido que se enviará
/// a la impresora ESC/POS). Solo lectura.
/// </summary>
public partial class TicketPreviewViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Encabezado de la tienda
    [ObservableProperty] private string _nombreTienda = string.Empty;
    [ObservableProperty] private string? _direccion;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _rfc;
    [ObservableProperty] private string? _mensajePie;

    // Datos de la venta
    [ObservableProperty] private string _folio = string.Empty;
    [ObservableProperty] private DateTime _fecha;
    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _descuentoGlobal;
    [ObservableProperty] private bool _tieneDescuentoGlobal;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private decimal _cambio;
    [ObservableProperty] private string _estado = string.Empty;
    [ObservableProperty] private string? _notas;

    public ObservableCollection<VentaLineaDetalleDto> Lineas { get; } = new();
    public ObservableCollection<PagoResumenDto> Pagos { get; } = new();

    public TicketPreviewViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task InicializarAsync(Guid ventaId)
    {
        using var scope = _scopeFactory.CreateScope();
        var ventas = scope.ServiceProvider.GetRequiredService<ConsultarVentasService>();
        var config = scope.ServiceProvider.GetRequiredService<ConfiguracionService>();

        var c = await config.ObtenerAsync();
        NombreTienda = c.NombreTienda;
        Direccion = c.Direccion;
        Telefono = c.Telefono;
        Rfc = c.Rfc;
        MensajePie = c.MensajePieTicket;

        var detalle = await ventas.ObtenerDetalleAsync(ventaId);
        if (detalle is null) return;

        Folio = detalle.Folio;
        Fecha = detalle.Fecha;
        Subtotal = detalle.Subtotal;
        DescuentoGlobal = detalle.DescuentoGlobal;
        TieneDescuentoGlobal = detalle.TieneDescuentoGlobal;
        Total = detalle.Total;
        Cambio = detalle.Cambio;
        Estado = detalle.Estado;
        Notas = detalle.Notas;

        Lineas.Clear();
        foreach (var l in detalle.Lineas) Lineas.Add(l);
        Pagos.Clear();
        foreach (var p in detalle.Pagos) Pagos.Add(p);
    }
}
