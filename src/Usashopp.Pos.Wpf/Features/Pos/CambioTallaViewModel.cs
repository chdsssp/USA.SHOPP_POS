using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Prenda de la venta original que puede regresar.</summary>
public partial class LineaCambioDevolver : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public int Disponible { get; init; }
    [ObservableProperty] private int _aDevolver;

    partial void OnADevolverChanged(int value)
    {
        if (value < 0) ADevolver = 0;
        else if (value > Disponible) ADevolver = Disponible;
    }
}

/// <summary>Prenda nueva que se lleva el cliente.</summary>
public partial class LineaCambioNueva : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    [ObservableProperty] private int _cantidad = 1;

    partial void OnCantidadChanged(int value)
    {
        if (value < 1) Cantidad = 1;
    }
}

/// <summary>
/// Cambio de talla en un solo flujo: escanear el folio del ticket → marcar qué regresa → escanear o
/// buscar lo que se lleva → cobrar o devolver la diferencia.
/// </summary>
public partial class CambioTallaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;
    private CancellationTokenSource? _ctsCalculo;
    private CancellationTokenSource? _ctsBusqueda;

    [ObservableProperty] private string _folio = string.Empty;
    [ObservableProperty] private VentaResumenDto? _venta;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string _busqueda = string.Empty;
    [ObservableProperty] private decimal _credito;
    [ObservableProperty] private decimal _totalNuevo;
    [ObservableProperty] private bool _ocupado;

    /// <summary>Si la diferencia es a favor del cliente: true = saldo a favor (nota de crédito), false = efectivo.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DevolverEnEfectivo))]
    private bool _devolverComoSaldo;

    public bool DevolverEnEfectivo { get => !DevolverComoSaldo; set => DevolverComoSaldo = !value; }

    public ObservableCollection<LineaCambioDevolver> Origen { get; } = new();
    public ObservableCollection<LineaCambioNueva> Nuevos { get; } = new();
    public ObservableCollection<ProductoBusquedaDto> Resultados { get; } = new();

    public bool HayVenta => Venta is not null;
    public decimal Diferencia => TotalNuevo - Credito;
    public bool HayQueCobrar => Diferencia > 0;
    public bool HayQueDevolver => Diferencia < 0;
    public string DiferenciaTexto => Diferencia switch
    {
        > 0 => $"Cobrar {Diferencia:C2}",
        < 0 => $"Devolver {-Diferencia:C2}",
        _ => "Sin diferencia"
    };

    /// <summary>Resultado al confirmar (para la pantalla de resultado del POS).</summary>
    public ResultadoCambioDto? Resultado { get; private set; }

    public event Action<bool>? Cerrar;
    public Action? EnfocarBusqueda { get; set; }

    public CambioTallaViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        Nuevos.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (LineaCambioNueva l in e.NewItems) l.PropertyChanged += (_, _) => _ = RecalcularAsync();
            _ = RecalcularAsync();
        };
    }

    partial void OnCreditoChanged(decimal value) => NotificarDiferencia();
    partial void OnTotalNuevoChanged(decimal value) => NotificarDiferencia();
    partial void OnVentaChanged(VentaResumenDto? value) => OnPropertyChanged(nameof(HayVenta));

    private void NotificarDiferencia()
    {
        OnPropertyChanged(nameof(Diferencia));
        OnPropertyChanged(nameof(HayQueCobrar));
        OnPropertyChanged(nameof(HayQueDevolver));
        OnPropertyChanged(nameof(DiferenciaTexto));
    }

    // ---------- Paso 1: venta original ----------

    [RelayCommand]
    private async Task BuscarVentaAsync()
    {
        Error = null;
        using var scope = _scopeFactory.CreateScope();
        var venta = await scope.ServiceProvider.GetRequiredService<ConsultarVentasService>().BuscarPorFolioAsync(Folio);
        if (venta is null) { Error = $"No se encontró la venta «{Folio}»."; return; }

        var cambio = scope.ServiceProvider.GetRequiredService<CambioService>();
        var valida = await cambio.ValidarVentaAsync(venta.Id);
        if (valida.EsFallo) { Error = valida.Error; return; }

        var lineas = await scope.ServiceProvider.GetRequiredService<DevolucionService>().ObtenerLineasAsync(venta.Id);
        Origen.Clear();
        foreach (var l in lineas.Where(l => l.Disponible > 0))
        {
            var linea = new LineaCambioDevolver
            {
                VarianteId = l.VarianteId, Descripcion = l.Descripcion, PrecioUnitario = l.PrecioUnitario, Disponible = l.Disponible,
                ADevolver = lineas.Count == 1 && l.Disponible == 1 ? 1 : 0 // una sola prenda: ya marcada
            };
            linea.PropertyChanged += (_, _) => _ = RecalcularAsync();
            Origen.Add(linea);
        }
        Venta = venta;
        await RecalcularAsync();
        EnfocarBusqueda?.Invoke();
    }

    [RelayCommand]
    private void MasDevolver(LineaCambioDevolver l) => l.ADevolver++;

    [RelayCommand]
    private void MenosDevolver(LineaCambioDevolver l) => l.ADevolver--;

    // ---------- Paso 2: lo que se lleva ----------

    partial void OnBusquedaChanged(string value) => _ = BuscarEnVivoAsync(value);

    private async Task BuscarEnVivoAsync(string texto)
    {
        _ctsBusqueda?.Cancel();
        var cts = _ctsBusqueda = new CancellationTokenSource();
        if (string.IsNullOrWhiteSpace(texto)) { Resultados.Clear(); return; }
        try
        {
            await Task.Delay(180, cts.Token);
            using var scope = _scopeFactory.CreateScope();
            var lista = await scope.ServiceProvider.GetRequiredService<BuscarProductosService>().PorTextoAsync(texto, 30, cts.Token);
            if (cts.IsCancellationRequested) return;
            Resultados.Clear();
            foreach (var r in lista) Resultados.Add(r);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Enter en la búsqueda: si es un código exacto (lector) se agrega directo.</summary>
    [RelayCommand]
    private async Task BuscarExactoAsync()
    {
        if (string.IsNullOrWhiteSpace(Busqueda)) return;
        using var scope = _scopeFactory.CreateScope();
        var exacto = await scope.ServiceProvider.GetRequiredService<BuscarProductosService>().PorCodigoBarrasAsync(Busqueda.Trim());
        if (exacto is not null) { AgregarNuevo(exacto); Busqueda = string.Empty; }
    }

    [RelayCommand]
    private void AgregarNuevo(ProductoBusquedaDto p)
    {
        var existente = Nuevos.FirstOrDefault(n => n.VarianteId == p.VarianteId);
        if (existente is not null) existente.Cantidad++;
        else Nuevos.Add(new LineaCambioNueva { VarianteId = p.VarianteId, Descripcion = p.Descripcion, Precio = p.Precio, Stock = p.Stock });
        Resultados.Clear();
        Busqueda = string.Empty;
    }

    [RelayCommand]
    private void QuitarNuevo(LineaCambioNueva l)
    {
        Nuevos.Remove(l);
        _ = RecalcularAsync();
    }

    // ---------- Totales y confirmación ----------

    private IReadOnlyList<DevolucionItemDto> Devueltos() =>
        Origen.Where(o => o.ADevolver > 0).Select(o => new DevolucionItemDto(o.VarianteId, o.ADevolver)).ToList();

    private IReadOnlyList<NuevaLineaDto> LineasNuevas() =>
        Nuevos.Select(n => new NuevaLineaDto(n.VarianteId, n.Cantidad)).ToList();

    private async Task RecalcularAsync()
    {
        if (Venta is null) return;
        _ctsCalculo?.Cancel();
        var cts = _ctsCalculo = new CancellationTokenSource();
        try
        {
            await Task.Delay(80, cts.Token);
            using var scope = _scopeFactory.CreateScope();
            var p = await scope.ServiceProvider.GetRequiredService<CambioService>().CalcularAsync(Venta.Id, Devueltos(), LineasNuevas(), cts.Token);
            if (cts.IsCancellationRequested) return;
            Credito = p.Credito;
            TotalNuevo = p.TotalNuevo;
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (Venta is null || Ocupado) return;
        Error = null;
        if (Devueltos().Count == 0) { Error = "Marca qué prenda regresa el cliente."; return; }
        await RecalcularAsync();

        IReadOnlyList<NuevoPagoDto>? pagos = null;
        if (HayQueCobrar)
        {
            var cobro = _dialogos.MostrarCobro(Diferencia);
            if (cobro is null) return;
            pagos = cobro.Pagos;
        }

        Ocupado = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<CambioService>().EjecutarAsync(new CambioDto(
                Venta.Id, Devueltos(), LineasNuevas(), pagos,
                DevolverComoSaldo ? MetodoReembolso.NotaCredito : MetodoReembolso.Efectivo));
            if (r.EsFallo) { Error = r.Error; return; }
            Resultado = r.Valor;
            Cerrar?.Invoke(true);
        }
        finally { Ocupado = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
