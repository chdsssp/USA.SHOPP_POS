using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Clientes;

/// <summary>Estado de cuenta de crédito (CxC) de un cliente: saldo y registro de abonos.</summary>
public partial class EstadoCuentaClienteViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid _clienteId;
    private bool _huboAbono;

    [ObservableProperty] private string _titulo = "Estado de cuenta";
    [ObservableProperty] private decimal _limiteCredito;
    [ObservableProperty] private decimal _cargos;
    [ObservableProperty] private decimal _abonos;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private decimal _disponible;
    [ObservableProperty] private decimal _saldoNotas;
    [ObservableProperty] private decimal _montoAbono;
    [ObservableProperty] private string? _nota;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _ocupado;

    public ObservableCollection<AbonoClienteDto> AbonosRecientes { get; } = new();

    public event Action<bool>? Cerrar;

    public EstadoCuentaClienteViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid clienteId, string nombre)
    {
        _clienteId = clienteId;
        Titulo = $"Estado de cuenta · {nombre}";
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteCuentaService>();
        var e = await servicio.ObtenerEstadoCreditoAsync(_clienteId);
        if (e is null) { Error = "El cliente no existe."; return; }

        LimiteCredito = e.LimiteCredito;
        Cargos = e.Cargos;
        Abonos = e.Abonos;
        Saldo = e.Saldo;
        Disponible = e.Disponible;
        SaldoNotas = e.SaldoNotasCredito;
        MontoAbono = e.Saldo > 0 ? e.Saldo : 0;
        AbonosRecientes.Clear();
        foreach (var a in e.AbonosRecientes) AbonosRecientes.Add(a);
    }

    [RelayCommand]
    private async Task RegistrarAbonoAsync()
    {
        if (Ocupado) return;
        Error = null;

        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteCuentaService>();
        Ocupado = true;
        try
        {
            var r = await servicio.RegistrarAbonoAsync(new RegistrarAbonoClienteDto(_clienteId, MontoAbono, Nota));
            if (r.EsFallo) { Error = r.Error; return; }
            _huboAbono = true;
            Nota = null;
            await CargarAsync();
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void CerrarDialogo() => Cerrar?.Invoke(_huboAbono);
}
