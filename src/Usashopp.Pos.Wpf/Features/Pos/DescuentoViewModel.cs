using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Resultado del diálogo de descuento (valor 0 = quitar descuento).</summary>
public record DescuentoResultado(TipoDescuento Tipo, decimal Valor);

public partial class DescuentoViewModel : ViewModelBase
{
    [ObservableProperty] private string _contexto = string.Empty;
    [ObservableProperty] private TipoDescuento _tipo = TipoDescuento.Porcentaje;
    [ObservableProperty] private decimal _valor;
    [ObservableProperty] private string? _error;

    public TipoDescuento[] Tipos { get; } = { TipoDescuento.Porcentaje, TipoDescuento.MontoFijo };

    public DescuentoResultado? Resultado { get; private set; }
    public event Action<bool>? Cerrar;

    public void Inicializar(string contexto, TipoDescuento? tipo, decimal valor)
    {
        Contexto = contexto;
        Tipo = tipo ?? TipoDescuento.Porcentaje;
        Valor = valor;
    }

    [RelayCommand]
    private void Aplicar()
    {
        Error = null;
        if (Valor < 0) { Error = "El valor no puede ser negativo."; return; }
        if (Tipo == TipoDescuento.Porcentaje && Valor > 100) { Error = "El porcentaje no puede superar 100."; return; }

        Resultado = new DescuentoResultado(Tipo, Valor);
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Quitar()
    {
        Resultado = new DescuentoResultado(TipoDescuento.Porcentaje, 0m);
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
