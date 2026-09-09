using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Clientes;

/// <summary>Historial de compras de un cliente.</summary>
public partial class HistorialClienteViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _titulo = "Historial de compras";

    public ObservableCollection<VentaResumenDto> Compras { get; } = new();

    public HistorialClienteViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Inicializar(Guid clienteId, string nombre)
    {
        Titulo = $"Historial de compras · {nombre}";
        _ = CargarAsync(clienteId);
    }

    private async Task CargarAsync(Guid clienteId)
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteCuentaService>();
        var lista = await servicio.ListarComprasAsync(clienteId);
        Compras.Clear();
        foreach (var c in lista) Compras.Add(c);
    }
}
