using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;
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

    /// <summary>Importe que se reembolsará (neto pagado, con descuentos).</summary>
    [ObservableProperty] private decimal _totalReembolso;

    /// <summary>Si el reembolso se emite como nota de crédito (saldo a favor) en vez de efectivo.</summary>
    [ObservableProperty] private bool _emitirNotaCredito;

    [ObservableProperty] private ClienteDto? _clienteSeleccionado;

    /// <summary>Saldo a favor que ya tiene el cliente seleccionado (notas activas).</summary>
    [ObservableProperty] private decimal _saldoClienteActual;

    public ObservableCollection<LineaDevolucionEditable> Lineas { get; } = new();
    public ObservableCollection<ClienteDto> Clientes { get; } = new();

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
            var clientesSrv = scope.ServiceProvider.GetRequiredService<ClienteService>();

            var lista = await servicio.ObtenerLineasAsync(_ventaId);
            Lineas.Clear();
            foreach (var l in lista)
            {
                var linea = new LineaDevolucionEditable
                {
                    VarianteId = l.VarianteId,
                    Descripcion = l.Descripcion,
                    PrecioUnitario = l.PrecioUnitario,
                    Vendida = l.Vendida,
                    Devuelta = l.Devuelta,
                    Disponible = l.Disponible
                };
                linea.PropertyChanged += LineaCambiada;
                Lineas.Add(linea);
            }

            Clientes.Clear();
            foreach (var c in await clientesSrv.ListarAsync())
                Clientes.Add(c);

            // Precarga el cliente de la venta, si lo tiene.
            var clienteVenta = await servicio.ObtenerClienteVentaAsync(_ventaId);
            if (clienteVenta is { } id)
                ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == id);
        }
        finally { Cargando = false; }
    }

    private void LineaCambiada(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LineaDevolucionEditable.ADevolver))
            _ = RecalcularReembolsoAsync();
    }

    partial void OnClienteSeleccionadoChanged(ClienteDto? value) => _ = CargarSaldoClienteAsync();

    private async Task CargarSaldoClienteAsync()
    {
        if (ClienteSeleccionado is null) { SaldoClienteActual = 0m; return; }
        using var scope = _scopeFactory.CreateScope();
        var notas = scope.ServiceProvider.GetRequiredService<NotaCreditoService>();
        SaldoClienteActual = await notas.SaldoDisponibleAsync(ClienteSeleccionado.Id);
    }

    private async Task RecalcularReembolsoAsync()
    {
        var items = ItemsSeleccionados();
        if (items.Count == 0) { TotalReembolso = 0m; return; }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
        TotalReembolso = await servicio.CalcularReembolsoAsync(_ventaId, items);
    }

    private List<DevolucionItemDto> ItemsSeleccionados() => Lineas
        .Where(l => l.ADevolver > 0)
        .Select(l => new DevolucionItemDto(l.VarianteId, l.ADevolver))
        .ToList();

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        Error = null;
        var items = ItemsSeleccionados();

        if (items.Count == 0) { Error = "Indica al menos una cantidad a devolver."; return; }

        var metodo = EmitirNotaCredito ? MetodoReembolso.NotaCredito : MetodoReembolso.Efectivo;
        if (metodo == MetodoReembolso.NotaCredito && ClienteSeleccionado is null)
        {
            Error = "Selecciona un cliente para emitir la nota de crédito.";
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<DevolucionService>();
        var r = await servicio.EjecutarAsync(_ventaId, items, metodo, ClienteSeleccionado?.Id);
        if (r.EsFallo) { Error = r.Error; return; }

        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
