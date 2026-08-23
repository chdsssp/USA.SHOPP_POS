using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Apertura de caja: captura del fondo inicial.</summary>
public partial class AbrirCajaViewModel : ViewModelBase
{
    [ObservableProperty] private decimal _fondoInicial;

    public event Action<bool>? Cerrar;

    [RelayCommand]
    private void Fijar(string monto)
    {
        if (decimal.TryParse(monto, out var valor))
            FondoInicial = valor;
    }

    [RelayCommand]
    private void Aceptar() => Cerrar?.Invoke(true);

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
