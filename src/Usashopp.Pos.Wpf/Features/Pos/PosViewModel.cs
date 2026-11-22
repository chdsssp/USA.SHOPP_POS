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
using Usashopp.Pos.Application.Configuracion;
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

    // Impuesto (desglose informativo; el precio normalmente ya lo incluye).
    private decimal _tasaImpuesto = 0.16m;
    private bool _impuestoIncluido = true;
    [ObservableProperty] private decimal _impuesto;
    [ObservableProperty] private string _impuestoEtiqueta = "IVA incluido";

    // ---- Resultado de la última venta (pantalla grande con el cambio) ----
    [ObservableProperty] private ResultadoVentaDto? _ultimoResultado;
    [ObservableProperty] private bool _mostrandoResultado;

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

    private readonly bool _tienePermisoDescuento;

    /// <summary>Un supervisor autorizó descuentos/edición de precio para la venta en curso.</summary>
    [ObservableProperty] private bool _descuentoAutorizado;

    /// <summary>
    /// Si se pueden aplicar descuentos y editar el precio en línea: por permiso propio
    /// (descuentos.aplicar) o por autorización de supervisor para la venta en curso.
    /// </summary>
    public bool PuedeDescuento => _tienePermisoDescuento || DescuentoAutorizado;

    /// <summary>Muestra el botón "Autorizar (supervisor)" cuando el cajero no tiene el permiso ni autorización.</summary>
    public bool MostrarBotonAutorizarDescuento => !_tienePermisoDescuento && !DescuentoAutorizado;


    public PosViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos,
        ICurrentUser currentUser)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _tienePermisoDescuento = currentUser.TienePermiso(Permisos.DescuentosAplicar);
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); ToastVisible = false; };
        _borradorTimer.Tick += async (_, _) => await GuardarBorradorAsync();
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
        await CargarConfiguracionAsync();
        await RefrescarCajaAsync();
        await CargarCategoriasAsync();
        await CargarGridAsync();
        await CargarClientesAsync();
        await RefrescarEnEsperaAsync();
        await OfrecerBorradorAsync();
    }

    private async Task CargarConfiguracionAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var config = await scope.ServiceProvider.GetRequiredService<ConfiguracionService>().ObtenerAsync();
        _tasaImpuesto = config.TasaImpuesto;
        _impuestoIncluido = config.ImpuestoIncluidoEnPrecio;
        ImpuestoEtiqueta = _impuestoIncluido
            ? $"IVA {config.TasaImpuesto:P0} incluido"
            : $"IVA {config.TasaImpuesto:P0}";
        RecalcularTotales();
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

    private CancellationTokenSource? _busquedaCts;

    /// <summary>Búsqueda en vivo: muestra resultados al escribir, sin esperar Enter.</summary>
    partial void OnTextoBusquedaChanged(string value) => _ = BuscarEnVivoAsync(value);

    private async Task BuscarEnVivoAsync(string? texto)
    {
        _busquedaCts?.Cancel();
        var cts = new CancellationTokenSource();
        _busquedaCts = cts;

        var t = (texto ?? "").Trim();
        if (t.Length == 0) { Resultados.Clear(); return; }

        try
        {
            await Task.Delay(180, cts.Token); // pequeño debounce para no consultar en cada tecla
            using var scope = _scopeFactory.CreateScope();
            var buscar = scope.ServiceProvider.GetRequiredService<BuscarProductosService>();
            var lista = await buscar.PorTextoAsync(t, ct: cts.Token);
            if (cts.Token.IsCancellationRequested) return;

            Resultados.Clear();
            foreach (var item in lista) Resultados.Add(item);
        }
        catch (OperationCanceledException) { /* reemplazada por una búsqueda más reciente */ }
    }

    // ---------------- Resultado de la venta ----------------

    /// <summary>Cierra la pantalla de resultado y deja el POS listo para la siguiente venta.</summary>
    [RelayCommand]
    private void NuevaVenta()
    {
        MostrandoResultado = false;
        PedirFocoBusqueda?.Invoke();
    }

    [RelayCommand]
    private async Task ReimprimirUltimaAsync()
    {
        if (UltimoResultado is not { } r) return;
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ConsultarVentasService>();
        var res = await servicio.ReimprimirAsync(r.VentaId);
        if (res.Exito)
        {
            // La impresión ya funcionó: se quita el aviso de "no se pudo imprimir".
            UltimoResultado = r with { Advertencias = r.Advertencias?.Where(a => !a.Contains("imprimir")).ToList() };
            MostrarToast($"Ticket {r.Folio} enviado a impresión");
        }
        else _dialogos.Mensaje(res.Error!);
    }

    [RelayCommand]
    private void VerTicketUltima()
    {
        if (UltimoResultado is { } r) _dialogos.MostrarVistaPreviaTicket(r.VentaId);
    }

    /// <summary>La vista enfoca la búsqueda cuando el ViewModel lo pide.</summary>
    public Action? PedirFocoBusqueda { get; set; }

    /// <summary>
    /// Código leído por el lector cuando el foco NO estaba en la búsqueda (detectado por ráfaga en
    /// la vista). Se agrega directo si coincide un código; si no, se muestra como búsqueda.
    /// </summary>
    public async Task ProcesarCodigoEscaneadoAsync(string codigo)
    {
        TextoBusqueda = codigo.Trim();
        await BuscarAsync();
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        var texto = TextoBusqueda.Trim();
        if (string.IsNullOrEmpty(texto)) return;

        // Escanear el siguiente producto cierra la pantalla de resultado de la venta anterior.
        if (MostrandoResultado) MostrandoResultado = false;

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
        if (lista.Count == 0)
            MostrarToast($"No se encontró «{texto}»");
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
        MostrandoResultado = false; // empezar a agregar = nueva venta
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
        // Solo las propiedades que captura el cajero disparan el recálculo (los importes que fija
        // RecalcularTotales también notifican, y no deben volver a disparar el cálculo).
        linea.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is { } p && LineaCarrito.PropiedadesDeEntrada.Contains(p))
                RecalcularTotales();
        };
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

    // ---------------- Ventas en espera (guardadas en la base) ----------------

    /// <summary>Cuántas ventas hay en espera (para el botón "En espera (N)").</summary>
    [ObservableProperty] private int _enEsperaCount;

    private async Task RefrescarEnEsperaAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        EnEsperaCount = await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().ContarAsync();
    }

    private NuevaVentaEsperaDto CarritoComoEspera(string? etiqueta) => new(
        etiqueta, ClienteSeleccionado?.Id, _descuentoGlobalTipo, _descuentoGlobalValor, Notas,
        Carrito.Select(l => new LineaEsperaDto(l.VarianteId, l.Descripcion, l.Sku, l.PrecioUnitario, l.PrecioVenta,
            l.Cantidad, l.DescuentoTipo, l.DescuentoValor, l.StockDisponible)).ToList());

    [RelayCommand]
    private async Task SuspenderVentaAsync()
    {
        if (Carrito.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>()
            .SuspenderAsync(CarritoComoEspera(ClienteSeleccionado?.Nombre));
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }

        ReiniciarVenta();
        await RefrescarEnEsperaAsync();
        MostrarToast("Venta puesta en espera");
    }

    [RelayCommand]
    private async Task RecuperarVentaAsync()
    {
        var v = _dialogos.MostrarVentasEnEspera();
        if (v is null) return;

        if (Carrito.Count > 0 &&
            !_dialogos.Confirmar("Se reemplazará el carrito actual con la venta en espera. ¿Continuar?", "Recuperar venta"))
            return;

        using var scope = _scopeFactory.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().RecuperarAsync(v.Id);
        await RefrescarEnEsperaAsync();
        if (r.EsFallo) { _dialogos.Mensaje(r.Error!); return; }
        await CargarEnCarritoAsync(r.Valor!);
    }

    /// <summary>Carga en el carrito una venta recuperada y muestra lo que cambió (precio/existencia).</summary>
    private async Task CargarEnCarritoAsync(VentaRecuperadaDto recuperada)
    {
        var v = recuperada.Venta;
        _restaurando = true;
        try
        {
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
            ClienteSeleccionado = v.ClienteId is { } id ? await BuscarClientePorIdAsync(id) : null;
        }
        finally { _restaurando = false; }

        RecalcularTotales();
        if (recuperada.Avisos.Count > 0)
            _dialogos.Mensaje("Revisa estos cambios desde que se guardó la venta:\n\n• " + string.Join("\n• ", recuperada.Avisos));
    }

    private Task<ClienteDto?> BuscarClientePorIdAsync(Guid id) =>
        Task.FromResult(Clientes.FirstOrDefault(c => c.Id == id));

    /// <summary>Deja el POS listo para una venta nueva (sin tocar la pantalla de resultado).</summary>
    private void ReiniciarVenta()
    {
        Carrito.Clear();
        _descuentoGlobalTipo = null;
        _descuentoGlobalValor = 0;
        ClienteSeleccionado = null;
        Notas = null;
        DescuentoAutorizado = false;
        RecalcularTotales();
    }

    // ---------------- Borrador automático del carrito ----------------
    // Si la app se cierra o se va la luz a media venta, el carrito se recupera al volver a entrar.

    private readonly DispatcherTimer _borradorTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private bool _restaurando;

    private void ProgramarBorrador()
    {
        if (_restaurando) return;
        _borradorTimer.Stop();
        _borradorTimer.Start();
    }

    private async Task GuardarBorradorAsync()
    {
        _borradorTimer.Stop();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>().GuardarBorradorAsync(CarritoComoEspera("Venta sin terminar"));
        }
        catch { /* el borrador es una red de seguridad: nunca debe estorbar la venta */ }
    }

    private async Task OfrecerBorradorAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<VentasEnEsperaService>();
        var borrador = await servicio.ObtenerBorradorAsync();
        if (borrador is null || Carrito.Count > 0) return;

        if (_dialogos.Confirmar(
                $"Quedó una venta sin terminar ({borrador.Venta.Articulos} art., {borrador.Venta.Total:C2}). ¿Recuperarla?",
                "Venta sin terminar"))
            await CargarEnCarritoAsync(borrador);
        else
            await servicio.EliminarBorradorAsync();
    }

    // ---------------- Descuentos ----------------

    partial void OnDescuentoAutorizadoChanged(bool value)
    {
        OnPropertyChanged(nameof(PuedeDescuento));
        OnPropertyChanged(nameof(MostrarBotonAutorizarDescuento));
    }

    /// <summary>Pide a un supervisor autorización para aplicar descuentos/editar precio en esta venta.</summary>
    [RelayCommand]
    private void AutorizarDescuento()
    {
        if (PuedeDescuento) return;
        if (_dialogos.MostrarAutorizacionSupervisor(
                Permisos.DescuentosAplicar, "aplicar descuentos o editar precios"))
        {
            DescuentoAutorizado = true;
            MostrarToast("Autorización de supervisor concedida.");
        }
    }

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

    /// <summary>
    /// Recalcula con <see cref="CalculadoraTotales"/> (Application), que usa las mismas entidades del
    /// dominio que registran la venta: lo que se ve en pantalla es exactamente lo que se cobra.
    /// </summary>
    private void RecalcularTotales()
    {
        var lineas = Carrito.ToList();
        var calc = CalculadoraTotales.Calcular(
            lineas.Select(l => new LineaCalculo(l.VarianteId, l.Cantidad, l.PrecioVenta, l.DescuentoTipo, l.DescuentoValor)).ToList(),
            _descuentoGlobalTipo, _descuentoGlobalValor, _tasaImpuesto, _impuestoIncluido);

        for (var i = 0; i < lineas.Count; i++)
        {
            lineas[i].DescuentoMonto = calc.Lineas[i].Descuento;
            lineas[i].Importe = calc.Lineas[i].Importe;
        }

        Subtotal = calc.Subtotal;
        DescuentoGlobalMonto = calc.DescuentoGlobal;
        Total = calc.Total;
        Impuesto = calc.Impuesto;
        CantidadArticulos = calc.Articulos;
        OnPropertyChanged(nameof(PuedeCobrar));
        OnPropertyChanged(nameof(CarritoVacio));
        OnPropertyChanged(nameof(TieneDescuentoGlobal));
        OnPropertyChanged(nameof(HayExcesoStock));
        ProgramarBorrador();
    }

    /// <summary>Alguna línea pide más piezas de las que hay en existencia.</summary>
    public bool HayExcesoStock => Carrito.Any(l => l.ExcedeStock);

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

        // Si hay cliente, consulta su crédito disponible y saldo a favor para ofrecerlos en el cobro.
        decimal creditoDisponible = 0, saldoNotas = 0;
        if (ClienteSeleccionado is { } cli)
        {
            using var scopeCred = _scopeFactory.CreateScope();
            var cuentas = scopeCred.ServiceProvider.GetRequiredService<ClienteCuentaService>();
            var estado = await cuentas.ObtenerEstadoCreditoAsync(cli.Id);
            if (estado is not null)
            {
                creditoDisponible = Math.Max(0, estado.Disponible);
                saldoNotas = estado.SaldoNotasCredito;
            }
        }

        var cobro = _dialogos.MostrarCobro(Total, ClienteSeleccionado is not null, creditoDisponible, saldoNotas);
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

        // Pantalla de resultado: el cambio queda visible en grande hasta que el cajero continúe
        // (antes era un toast de 3 s). Incluye avisos si falló la impresora o el cajón.
        UltimoResultado = resultado.Valor!;
        MostrandoResultado = true;

        ReiniciarVenta(); // la autorización de supervisor aplica solo a la venta en curso
        await GuardarBorradorAsync(); // carrito vacío = se borra el borrador
        await CargarGridAsync(); // el stock cambió
    }
}
