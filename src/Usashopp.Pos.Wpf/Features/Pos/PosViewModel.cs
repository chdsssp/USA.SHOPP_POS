using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Pantalla de Punto de Venta: búsqueda (teclado/lector) y grid táctil, carrito, apertura
/// de caja y cobro. Habla con los servicios de Application resueltos por scope.
/// </summary>
public partial class PosViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    [ObservableProperty] private bool _modoBusqueda = true;
    [ObservableProperty] private string _textoBusqueda = string.Empty;
    [ObservableProperty] private bool _cajaAbierta;
    [ObservableProperty] private string _cajaTexto = "Caja cerrada";
    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private int _cantidadArticulos;
    [ObservableProperty] private Guid? _categoriaSeleccionadaId;
    [ObservableProperty] private bool _toastVisible;
    [ObservableProperty] private string _toastMensaje = string.Empty;

    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(3) };

    public ObservableCollection<ProductoBusquedaDto> Resultados { get; } = new();
    public ObservableCollection<ProductoBusquedaDto> ProductosGrid { get; } = new();
    public ObservableCollection<CategoriaChip> Chips { get; } = new();
    public ObservableCollection<LineaCarrito> Carrito { get; } = new();

    public bool ModoGrid => !ModoBusqueda;
    public bool PuedeCobrar => CajaAbierta && Carrito.Count > 0;
    public bool CarritoVacio => Carrito.Count == 0;

    public PosViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); ToastVisible = false; };
        WeakReferenceMessenger.Default.Register<CajaEstadoCambiadoMessage>(this, (_, _) => _ = RefrescarCajaAsync());
        _ = InicializarAsync();
    }

    private void MostrarToast(string mensaje)
    {
        ToastMensaje = mensaje;
        ToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private async Task InicializarAsync()
    {
        await RefrescarCajaAsync();
        await CargarCategoriasAsync();
        await CargarGridAsync();
    }

    partial void OnModoBusquedaChanged(bool value) => OnPropertyChanged(nameof(ModoGrid));
    partial void OnCajaAbiertaChanged(bool value) => OnPropertyChanged(nameof(PuedeCobrar));

    // ---------------- Modo ----------------

    [RelayCommand]
    private void UsarBusqueda() => ModoBusqueda = true;

    [RelayCommand]
    private void UsarGrid() => ModoBusqueda = false;

    // ---------------- Caja ----------------

    private async Task RefrescarCajaAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
        var sesion = await caja.ObtenerAbiertaAsync();
        CajaAbierta = sesion is not null;
        CajaTexto = sesion is null ? "Caja cerrada" : $"Caja abierta · Fondo {sesion.FondoInicial:C0}";
    }

    [RelayCommand]
    private async Task AbrirCajaAsync()
    {
        var fondo = _dialogos.MostrarAbrirCaja();
        if (fondo is null) return;

        using var scope = _scopeFactory.CreateScope();
        var caja = scope.ServiceProvider.GetRequiredService<CajaService>();
        var resultado = await caja.AbrirAsync(fondo.Value);
        if (resultado.EsFallo)
        {
            _dialogos.Mensaje(resultado.Error!);
            return;
        }
        await RefrescarCajaAsync();
        WeakReferenceMessenger.Default.Send(new CajaEstadoCambiadoMessage());
    }

    // ---------------- Búsqueda / grid ----------------

    [RelayCommand]
    private async Task BuscarAsync()
    {
        var texto = TextoBusqueda.Trim();
        if (string.IsNullOrEmpty(texto)) return;

        using var scope = _scopeFactory.CreateScope();
        var buscar = scope.ServiceProvider.GetRequiredService<BuscarProductosService>();

        // Coincidencia exacta por código de barras (lector) → agrega directo.
        var porCodigo = await buscar.PorCodigoBarrasAsync(texto);
        if (porCodigo is not null)
        {
            AgregarAlCarrito(porCodigo);
            TextoBusqueda = string.Empty;
            Resultados.Clear();
            return;
        }

        var lista = await buscar.PorTextoAsync(texto);
        Resultados.Clear();
        foreach (var item in lista)
            Resultados.Add(item);
    }

    private async Task CargarCategoriasAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var categorias = scope.ServiceProvider.GetRequiredService<CategoriaService>();
        var lista = await categorias.ListarAsync();

        Chips.Clear();
        Chips.Add(new CategoriaChip(null, "Todas") { Activo = true });
        foreach (var c in lista)
            Chips.Add(new CategoriaChip(c.Id, c.Nombre));
    }

    private async Task CargarGridAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var buscar = scope.ServiceProvider.GetRequiredService<BuscarProductosService>();
        var lista = await buscar.ParaGridAsync(CategoriaSeleccionadaId);
        ProductosGrid.Clear();
        foreach (var item in lista)
            ProductosGrid.Add(item);
    }

    [RelayCommand]
    private async Task FiltrarCategoriaAsync(CategoriaChip chip)
    {
        CategoriaSeleccionadaId = chip.Id;
        foreach (var c in Chips)
            c.Activo = ReferenceEquals(c, chip);
        await CargarGridAsync();
    }

    // ---------------- Carrito ----------------

    [RelayCommand]
    private void Agregar(ProductoBusquedaDto producto) => AgregarAlCarrito(producto);

    private void AgregarAlCarrito(ProductoBusquedaDto producto)
    {
        var existente = Carrito.FirstOrDefault(l => l.VarianteId == producto.VarianteId);
        if (existente is not null)
            existente.Cantidad++;
        else
            Carrito.Add(new LineaCarrito
            {
                VarianteId = producto.VarianteId,
                Descripcion = producto.Descripcion,
                Sku = producto.Sku,
                PrecioUnitario = producto.Precio,
                StockDisponible = producto.Stock,
                Cantidad = 1
            });

        RecalcularTotales();
    }

    [RelayCommand]
    private void Incrementar(LineaCarrito linea)
    {
        linea.Cantidad++;
        RecalcularTotales();
    }

    [RelayCommand]
    private void Decrementar(LineaCarrito linea)
    {
        if (linea.Cantidad > 1)
            linea.Cantidad--;
        else
            Carrito.Remove(linea);
        RecalcularTotales();
    }

    [RelayCommand]
    private void Quitar(LineaCarrito linea)
    {
        Carrito.Remove(linea);
        RecalcularTotales();
    }

    [RelayCommand]
    private void LimpiarCarrito()
    {
        Carrito.Clear();
        RecalcularTotales();
    }

    private void RecalcularTotales()
    {
        Subtotal = Carrito.Sum(l => l.Importe);
        Total = Subtotal; // El IVA se asume incluido en el precio (configurable).
        CantidadArticulos = Carrito.Sum(l => l.Cantidad);
        OnPropertyChanged(nameof(PuedeCobrar));
        OnPropertyChanged(nameof(CarritoVacio));
    }

    // ---------------- Cobro ----------------

    [RelayCommand]
    private async Task CobrarAsync()
    {
        if (!CajaAbierta)
        {
            _dialogos.Mensaje("Abre la caja antes de cobrar.");
            return;
        }
        if (Carrito.Count == 0) return;

        var cobro = _dialogos.MostrarCobro(Total);
        if (cobro is null) return;

        var dto = new NuevaVentaDto(
            Carrito.Select(l => new NuevaLineaDto(l.VarianteId, l.Cantidad)).ToList(),
            cobro.Pagos,
            Imprimir: true,
            AbrirCajon: true);

        using var scope = _scopeFactory.CreateScope();
        var registrar = scope.ServiceProvider.GetRequiredService<RegistrarVentaService>();
        var resultado = await registrar.EjecutarAsync(dto);

        if (resultado.EsFallo)
        {
            _dialogos.Mensaje(resultado.Error!);
            return;
        }

        var venta = resultado.Valor!;
        MostrarToast($"Venta {venta.Folio} · Total {venta.Total:C2} · Cambio {venta.Cambio:C2}");

        Carrito.Clear();
        RecalcularTotales();
        await CargarGridAsync(); // el stock cambió
    }
}
