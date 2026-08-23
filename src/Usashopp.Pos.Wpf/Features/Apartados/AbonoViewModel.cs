using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Apartados;
using Usashopp.Pos.Application.Apartados.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Apartados;

/// <summary>Registrar un abono a un apartado.</summary>
public partial class AbonoViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _apartadoId;

    [ObservableProperty] private string _folio = string.Empty;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private decimal _monto;
    [ObservableProperty] private MetodoPago _metodo = MetodoPago.Efectivo;
    [ObservableProperty] private string? _error;

    public MetodoPago[] Metodos { get; } = { MetodoPago.Efectivo, MetodoPago.Tarjeta, MetodoPago.Transferencia };

    public event Action<bool>? Cerrar;

    public AbonoViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid apartadoId, string folio, decimal saldo)
    {
        _apartadoId = apartadoId;
        Folio = folio;
        Saldo = saldo;
        Monto = saldo;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
        var r = await servicio.AbonarAsync(new NuevoAbonoDto(_apartadoId, Monto, Metodo));
        if (r.EsFallo) { Error = r.Error; return; }
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
