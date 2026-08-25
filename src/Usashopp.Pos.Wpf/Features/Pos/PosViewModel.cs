using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Caja;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Application.Ventas.Dtos;
using Usashopp.Pos.Domain.Enums;
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
    private readonly VentasEnEsperaStore _espera;

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
    [ObservableProperty] private ClienteDto? _clienteSeleccionado;
    [ObservableProperty] private decimal _descuentoGlobalMonto;
    [ObservableProperty] private string? _notas;

    private TipoDescuento? _descuentoGlobalTipo;
    private decimal _descuentoGlobalValor;

    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(3) };

    public ObservableCollection<ProductoBusquedaDto> Resultados { get; } = new();
    public ObservableCollection<ProductoBusquedaDto> ProductosGrid { get; } = new();
    public ObservableCollection<CategoriaChip> Chips { get; } = new();
    public ObservableCollection<LineaCarrito> Carrito { get; } = new();
    public ObservableCollection<ClienteDto> Clientes { get; } = new();

    public bool ModoGrid => !ModoBusqueda;
    public bool PuedeCobrar => CajaAbierta && Carrito.Count > 0;
    public bool CarritoVacio => Carrito.Count == 0;
    public bool TieneDescuentoGlobal => DescuentoGlobalMonto > 0;

    /// <summary>Si el usuario puede aplicar descuentos (permiso descuentos.aplicar).</summary>
    public bool PuedeDescuento { get; }

    /// <summary>Ventas suspendidas (para el botón "En espera").</summary>
    public VentasEnEsperaStore Espera => _espera;

    public PosViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos,
        ICurrentUser currentUser, VentasEnEsperaStore espera)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _espera = espera;
        PuedeDescuento = currentUser.TienePermiso(Permisos.DescuentosAplicar);
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
        await CargarClientesAsync();
    }

    private async Task CargarClientesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteService>();
        var lista = await servicio.ListarAsync();
        Clientes.Clear();
        foreach (var c in lista) Clientes.Add(c);
    }

    [RelayCommand]
    private async Task NuevoClienteRapido()
    {
        var previos = Clientes.Select(c => c.Id).ToHashSet();
        if (!_dialogos.MostrarEditorCliente(null)) return;

        await CargarClientesAsync();
        // Selecciona automáticamente el cliente recién creado.
        ClienteSeleccionado = Clientes.FirstOrDefault(c => !previos.Contains(c.Id)) ?? ClienteSeleccionado;
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
            EngancharLinea(new LineaCarrito
            {
                VarianteId = producto.VarianteId,
                Descripcion = producto.Descripcion,
                Sku = producto.Sku,
                PrecioUnitario = producto.Precio,
                PrecioVenta = producto.Precio,
                StockDisponible = producto.Stock,
                Cantidad = 1
            });

        RecalcularTotales();
    }

    /// <summary>Agrega la línea al carrito y recalcula totales cuando cambie (cantidad/precio/descuento).</summary>
    private void EngancharLinea(LineaCarrito linea)
    {
        linea.PropertyChanged += (_, _) => RecalcularTotales();
        Carrito.Add(linea);
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
        _descuentoGlobalTipo = null;
        _descuentoGlobalValor = 0;
        RecalcularTotales();
    }

    // ---------------- Ventas en espera ----------------

    [RelayCommand]
    private void SuspenderVenta()
    {
        if (Carrito.Count == 0) return;

        _espera.Items.Add(new VentaEnEspera
        {
            Etiqueta = ClienteSeleccionado?.Nombre ?? $"Ticket {DateTime.Now:HH:mm}",
            ClienteId = ClienteSeleccionado?.Id,
            ClienteNombre = ClienteSeleccionado?.Nombre,
            DescuentoGlobalTipo = _descuentoGlobalTipo,
            DescuentoGlobalValor = _descuentoGlobalValor,
            Notas = Notas,
            Total = Total,
            Lineas = Carrito.Select(l => new LineaEnEspera
            {
                VarianteId = l.VarianteId,
                Descripcion = l.Descripcion,
                Sku = l.Sku,
                PrecioUnitario = l.PrecioUnitario,
                PrecioVenta = l.PrecioVenta,
                StockDisponible = l.StockDisponible,
                Cantidad = l.Cantidad,
                DescuentoTipo = l.DescuentoTipo,
                DescuentoValor = l.DescuentoValor
            }).ToList()
        });

        Carrito.Clear();
        _descuentoGlobalTipo = null;
        _descuentoGlobalValor = 0;
        ClienteSeleccionado = null;
        Notas = null;
        RecalcularTotales();
        MostrarToast("Venta puesta en espera");
    }

    [RelayCommand]
    private void RecuperarVenta()
    {
        var v = _dialogos.MostrarVentasEnEspera();
        if (v is null) return;

        if (Carrito.Count > 0 &&
            !_dialogos.Confirmar("Se reemplazará el carrito actual con la venta en espera. ¿Continuar?", "Recuperar venta"))
            return;

        Carrito.Clear();
        foreach (var l in v.Lineas)
            EngancharLinea(new LineaCarrito
            {
                VarianteId = l.VarianteId,
                Descripcion = l.Descripcion,
                Sku = l.Sku,
                PrecioUnitario = l.PrecioUnitario,
                PrecioVenta = l.PrecioVenta,
                StockDisponible = l.StockDisponible,
                Cantidad = l.Cantidad,
                DescuentoTipo = l.DescuentoTipo,
                DescuentoValor = l.DescuentoValor
            });

        _descuentoGlobalTipo = v.DescuentoGlobalTipo;
        _descuentoGlobalValor = v.DescuentoGlobalValor;
        Notas = v.Notas;
        ClienteSeleccionado = v.ClienteId is { } id ? Clientes.FirstOrDefault(c => c.Id == id) : null;

        _espera.Items.Remove(v);
        RecalcularTotales();
    }

    // ---------------- Descuentos ----------------

    [RelayCommand]
    private void DescontarLinea(LineaCarrito linea)
    {
        if (!PuedeDescuento) { _dialogos.Mensaje("No tienes permiso para aplicar descuentos."); return; }
        var res = _dialogos.MostrarDescuento($"Descuento a: {linea.Descripcion}", linea.DescuentoTipo, linea.DescuentoValor);
        if (res is null) return;
        linea.DescuentoTipo = res.Valor > 0 ? res.Tipo : null;
        linea.DescuentoValor = res.Valor;
        RecalcularTotales();
    }

    [RelayCommand]
    private void DescuentoGlobal()
    {
        if (!PuedeDescuento) { _dialogos.Mensaje("No tienes permiso para aplicar descuentos."); return; }
        if (Carrito.Count == 0) return;
        var res = _dialogos.MostrarDescuento("Descuento a toda la venta", _descuentoGlobalTipo, _descuentoGlobalValor);
        if (res is null) return;
        _descuentoGlobalTipo = res.Valor > 0 ? res.Tipo : null;
        _descuentoGlobalValor = res.Valor;
        RecalcularTotales();
    }

    private void RecalcularTotales()
    {
        Subtotal = Carrito.Sum(l => l.Importe);
        DescuentoGlobalMonto = CalcularDescuentoGlobal(Subtotal);
        Total = Subtotal - DescuentoGlobalMonto; // El IVA se asume incluido en el precio (configurable).
        CantidadArticulos = Carrito.Sum(l => l.Cantidad);
        OnPropertyChanged(nameof(PuedeCobrar));
        OnPropertyChanged(nameof(CarritoVacio));
        OnPropertyChanged(nameof(TieneDescuentoGlobal));
    }

    private decimal CalcularDescuentoGlobal(decimal baseImporte)
    {
        if (_descuentoGlobalTipo is null || _descuentoGlobalValor <= 0) return 0m;
        var monto = _descuentoGlobalTipo == TipoDescuento.Porcentaje
            ? baseImporte * (_descuentoGlobalValor / 100m)
            : _descuentoGlobalValor;
        return Math.Round(Math.Min(monto, baseImporte), 2);
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
            Carrito.Select(l => new NuevaLineaDto(l.VarianteId, l.Cantidad, l.DescuentoTipo, l.DescuentoValor, l.PrecioVenta)).ToList(),
            cobro.Pagos,
            ClienteId: ClienteSeleccionado?.Id,
            DescuentoGlobalTipo: _descuentoGlobalTipo,
            DescuentoGlobalValor: _descuentoGlobalValor,
            Notas: string.IsNullOrWhiteSpace(Notas) ? null : Notas!.Trim(),
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
        _descuentoGlobalTipo = null;
        _descuentoGlobalValor = 0;
        ClienteSeleccionado = null;
        Notas = null;
        RecalcularTotales();
        await CargarGridAsync(); // el stock cambió
    }
}
