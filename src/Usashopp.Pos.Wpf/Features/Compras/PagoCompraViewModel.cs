using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Compras;
using Usashopp.Pos.Application.Compras.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Cuentas por pagar de una compra: saldo, historial de abonos y registro de pagos.</summary>
public partial class PagoCompraViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _compraId;
    private bool _huboPago;

    [ObservableProperty] private string _titulo = "Cuentas por pagar";
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private decimal _pagado;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private decimal _montoPago;
    [ObservableProperty] private string? _nota;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<PagoCompraDto> Pagos { get; } = new();

    public event Action<bool>? Cerrar;

    public PagoCompraViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid compraId)
    {
        _compraId = compraId;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CuentasPorPagarService>();
        var estado = await servicio.ObtenerEstadoCuentaAsync(_compraId);
        if (estado is null) { Error = "La compra no existe."; return; }

        Titulo = $"Cuentas por pagar · {estado.Folio} · {estado.Proveedor}";
        Total = estado.Total;
        Pagado = estado.Pagado;
        Saldo = estado.Saldo;
        MontoPago = estado.Saldo; // por defecto, saldar
        Pagos.Clear();
        foreach (var p in estado.Pagos) Pagos.Add(p);
    }

    [RelayCommand]
    private async Task RegistrarPagoAsync()
    {
        if (Ocupado) return;
        Error = null;

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<CuentasPorPagarService>();
        Ocupado = true;
        try
        {
            var r = await servicio.RegistrarPagoAsync(new RegistrarPagoCompraDto(_compraId, MontoPago, Nota));
            if (r.EsFallo) { Error = r.Error; return; }
            _huboPago = true;
            Nota = null;
            await CargarAsync();
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void CerrarDialogo() => Cerrar?.Invoke(_huboPago);
}
