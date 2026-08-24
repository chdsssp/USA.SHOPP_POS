using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Cobro de una venta: método (efectivo/tarjeta), teclado numérico, montos rápidos
/// y cálculo de cambio.
/// </summary>
public partial class CobroViewModel : ViewModelBase
{
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private string _entrada = "0";
    [ObservableProperty] private MetodoPago _metodo = MetodoPago.Efectivo;
    [ObservableProperty] private string? _notas;

    public CobroResultado? Resultado { get; private set; }
    public event Action<bool>? Cerrar;

    public void Inicializar(decimal total)
    {
        Total = total;
        Metodo = MetodoPago.Efectivo;
        Entrada = "0";
        Notas = null;
    }

    public decimal MontoRecibido => decimal.TryParse(Entrada, out var v) ? v : 0m;
    public decimal Cambio => Metodo == MetodoPago.Efectivo ? Math.Max(0, MontoRecibido - Total) : 0m;
    public decimal Faltante => Math.Max(0, Total - MontoRecibido);
    public bool EsEfectivo => Metodo == MetodoPago.Efectivo;
    public bool EsTarjeta => Metodo == MetodoPago.Tarjeta;
    public bool PuedeConfirmar => Metodo == MetodoPago.Tarjeta || MontoRecibido >= Total;

    partial void OnEntradaChanged(string value) => NotificarCalculos();
    partial void OnMetodoChanged(MetodoPago value) => NotificarCalculos();
    partial void OnTotalChanged(decimal value) => NotificarCalculos();

    private void NotificarCalculos()
    {
        OnPropertyChanged(nameof(MontoRecibido));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(Faltante));
        OnPropertyChanged(nameof(EsEfectivo));
        OnPropertyChanged(nameof(EsTarjeta));
        OnPropertyChanged(nameof(PuedeConfirmar));
    }

    [RelayCommand]
    private void Tecla(string digito)
    {
        Entrada = Entrada == "0" ? digito : Entrada + digito;
    }

    [RelayCommand]
    private void Borrar()
    {
        Entrada = Entrada.Length <= 1 ? "0" : Entrada[..^1];
    }

    [RelayCommand]
    private void MontoRapido(string monto) => Entrada = monto;

    [RelayCommand]
    private void MontoExacto() => Entrada = ((int)Math.Ceiling(Total)).ToString();

    [RelayCommand]
    private void UsarEfectivo() => Metodo = MetodoPago.Efectivo;

    [RelayCommand]
    private void UsarTarjeta()
    {
        Metodo = MetodoPago.Tarjeta;
        Entrada = ((int)Math.Ceiling(Total)).ToString(); // tarjeta: monto exacto
    }

    [RelayCommand]
    private void Confirmar()
    {
        if (!PuedeConfirmar)
            return;

        var montoPago = Metodo == MetodoPago.Efectivo ? MontoRecibido : Total;
        var pagos = new List<NuevoPagoDto> { new(Metodo, montoPago) };
        Resultado = new CobroResultado(pagos, Cambio, string.IsNullOrWhiteSpace(Notas) ? null : Notas!.Trim());
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
