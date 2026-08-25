using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Cobro de una venta: soporta <b>pago mixto</b> (varios pagos con distinto método),
/// teclado numérico, montos rápidos y cálculo de cambio y faltante.
/// </summary>
public partial class CobroViewModel : ViewModelBase
{
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private string _entrada = "0";
    [ObservableProperty] private MetodoPago _metodo = MetodoPago.Efectivo;

    /// <summary>Pagos ya agregados a la venta (para mezclar métodos).</summary>
    public ObservableCollection<PagoCapturado> Pagos { get; } = new();

    public CobroResultado? Resultado { get; private set; }
    public event Action<bool>? Cerrar;

    public void Inicializar(decimal total)
    {
        Total = total;
        Metodo = MetodoPago.Efectivo;
        Entrada = "0";
        Pagos.Clear();
        NotificarCalculos();
    }

    // ---- Cálculos ----
    public decimal MontoRecibido => decimal.TryParse(Entrada, out var v) ? v : 0m;
    public decimal TotalCapturado => Pagos.Sum(p => p.Monto);
    /// <summary>Total cubierto = pagos agregados + lo que está capturándose ahora.</summary>
    public decimal Cubierto => TotalCapturado + MontoRecibido;
    /// <summary>Lo que falta por cubrir sin contar lo que se está tecleando (para autollenado).</summary>
    public decimal Restante => Math.Max(0, Total - TotalCapturado);
    public decimal Faltante => Math.Max(0, Total - Cubierto);
    public decimal Cambio => Math.Max(0, Cubierto - Total);
    public bool HayPagos => Pagos.Count > 0;
    public bool EsEfectivo => Metodo == MetodoPago.Efectivo;
    public bool EsTarjeta => Metodo == MetodoPago.Tarjeta;
    public bool PuedeAgregar => MontoRecibido > 0 && Faltante > 0;
    public bool PuedeConfirmar => Cubierto >= Total && (HayPagos || MontoRecibido > 0);

    partial void OnEntradaChanged(string value) => NotificarCalculos();
    partial void OnMetodoChanged(MetodoPago value) => NotificarCalculos();
    partial void OnTotalChanged(decimal value) => NotificarCalculos();

    private void NotificarCalculos()
    {
        OnPropertyChanged(nameof(MontoRecibido));
        OnPropertyChanged(nameof(TotalCapturado));
        OnPropertyChanged(nameof(Cubierto));
        OnPropertyChanged(nameof(Restante));
        OnPropertyChanged(nameof(Faltante));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(HayPagos));
        OnPropertyChanged(nameof(EsEfectivo));
        OnPropertyChanged(nameof(EsTarjeta));
        OnPropertyChanged(nameof(PuedeAgregar));
        OnPropertyChanged(nameof(PuedeConfirmar));
    }

    // ---- Teclado ----
    [RelayCommand]
    private void Tecla(string digito) => Entrada = Entrada == "0" ? digito : Entrada + digito;

    [RelayCommand]
    private void Borrar() => Entrada = Entrada.Length <= 1 ? "0" : Entrada[..^1];

    [RelayCommand]
    private void MontoRapido(string monto) => Entrada = monto;

    /// <summary>Captura el importe justo que falta por cobrar.</summary>
    [RelayCommand]
    private void MontoExacto() => Entrada = FormatearImporte(Restante > 0 ? Restante : Total);

    [RelayCommand]
    private void UsarEfectivo() => Metodo = MetodoPago.Efectivo;

    [RelayCommand]
    private void UsarTarjeta()
    {
        Metodo = MetodoPago.Tarjeta;
        // Con tarjeta se cobra el importe exacto que falta.
        Entrada = FormatearImporte(Restante > 0 ? Restante : Total);
    }

    /// <summary>Formatea un importe para el cuadro de entrada (2 decimales si aplica).</summary>
    private static string FormatearImporte(decimal v) =>
        v == Math.Truncate(v) ? ((long)v).ToString() : v.ToString("0.00");

    // ---- Pago mixto ----
    [RelayCommand]
    private void AgregarPago()
    {
        if (MontoRecibido <= 0) return;
        // Con tarjeta no tiene sentido cobrar de más como abono intermedio: se acota al faltante.
        var monto = Metodo == MetodoPago.Tarjeta ? Math.Min(MontoRecibido, Faltante > 0 ? Faltante : MontoRecibido) : MontoRecibido;
        if (monto <= 0) return;

        Pagos.Add(new PagoCapturado(Metodo, monto));
        Entrada = "0";
        NotificarCalculos();
    }

    [RelayCommand]
    private void QuitarPago(PagoCapturado pago)
    {
        Pagos.Remove(pago);
        NotificarCalculos();
    }

    [RelayCommand]
    private void Confirmar()
    {
        if (!PuedeConfirmar) return;

        var pagos = Pagos.Select(p => new NuevoPagoDto(p.Metodo, p.Monto)).ToList();

        // Lo que quedó capturándose (sin "Agregar") cuenta como el último pago.
        if (MontoRecibido > 0)
            pagos.Add(new NuevoPagoDto(Metodo, MontoRecibido));

        Resultado = new CobroResultado(pagos, Cambio);
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
