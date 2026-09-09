using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Fila del diálogo de recepción: cuánto recibir de una línea pendiente.</summary>
public partial class RecepcionLineaEditable : ObservableObject
{
    public Guid DetalleId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public int Recibido { get; init; }
    public int Pendiente { get; init; }

    [ObservableProperty] private int _aRecibir;

    partial void OnARecibirChanged(int value)
    {
        if (value < 0) ARecibir = 0;
        else if (value > Pendiente) ARecibir = Pendiente;
    }
}
