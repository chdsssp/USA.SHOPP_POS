using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Lista de ventas en espera (guardadas en la base) para recuperar una o descartarla.</summary>
public partial class VentasEnEsperaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    public ObservableCollection<VentaEsperaDto> Items { get; } = new();

    /// <summary>La venta elegida para recuperar (null si se canceló).</summary>
    public VentaEsperaDto? Seleccionada { get; private set; }

    public event Action<bool>? Cerrar;

    public VentasEnEsperaViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var lista = await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().ListarAsync();
        Items.Clear();
        foreach (var v in lista) Items.Add(v);
    }

    [RelayCommand]
    private void Recuperar(VentaEsperaDto venta)
    {
        Seleccionada = venta;
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private async Task DescartarAsync(VentaEsperaDto venta)
    {
        if (!_dialogos.Confirmar($"¿Descartar «{venta.Etiqueta}» ({venta.Articulos} art.)?", "Descartar venta en espera"))
            return;
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().DescartarAsync(venta.Id);
        Items.Remove(venta);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
