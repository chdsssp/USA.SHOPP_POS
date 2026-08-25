using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Lista de ventas en espera para recuperar una o descartarla.</summary>
public partial class VentasEnEsperaViewModel : ViewModelBase
{
    private readonly VentasEnEsperaStore _store;

    public ObservableCollection<VentaEnEspera> Items => _store.Items;

    /// <summary>La venta elegida para recuperar (null si se canceló).</summary>
    public VentaEnEspera? Seleccionada { get; private set; }

    public event Action<bool>? Cerrar;

    public VentasEnEsperaViewModel(VentasEnEsperaStore store) => _store = store;

    [RelayCommand]
    private void Recuperar(VentaEnEspera venta)
    {
        Seleccionada = venta;
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Descartar(VentaEnEspera venta) => _store.Items.Remove(venta);

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
