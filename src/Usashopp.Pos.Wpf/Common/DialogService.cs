using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Features.Apartados;
using Usashopp.Pos.Wpf.Features.Categorias;
using Usashopp.Pos.Wpf.Features.Clientes;
using Usashopp.Pos.Wpf.Features.Compras;
using Usashopp.Pos.Wpf.Features.Inventario;
using Usashopp.Pos.Wpf.Features.Pos;
using Usashopp.Pos.Wpf.Features.Proveedores;
using Usashopp.Pos.Wpf.Features.Usuarios;
using Usashopp.Pos.Wpf.Features.Ventas;

namespace Usashopp.Pos.Wpf.Common;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services) => _services = services;

    public bool MostrarEditorProducto(Guid? productoId = null)
    {
        var ventana = _services.GetRequiredService<ProductoEditorWindow>();
        if (ventana.DataContext is ProductoEditorViewModel vm) vm.Inicializar(productoId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarAjusteStock(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<AjusteStockWindow>();
        if (ventana.DataContext is AjusteStockViewModel vm)
            vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public CobroResultado? MostrarCobro(decimal total, bool clienteAsignado = false, decimal creditoDisponible = 0, decimal saldoNotas = 0)
    {
        var ventana = _services.GetRequiredService<CobroWindow>();
        if (ventana.DataContext is CobroViewModel vm)
        {
            vm.Inicializar(total, clienteAsignado, creditoDisponible, saldoNotas);
            ventana.Owner = System.Windows.Application.Current.MainWindow;
            return HojaTactil.Mostrar(ventana) == true ? vm.Resultado : null;
        }
        return null;
    }

    public Application.Ventas.VentaEsperaDto? MostrarVentasEnEspera()
    {
        var ventana = _services.GetRequiredService<VentasEnEsperaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true && ventana.DataContext is VentasEnEsperaViewModel vm
            ? vm.Seleccionada
            : null;
    }

    public decimal? MostrarAbrirCaja()
    {
        var ventana = _services.GetRequiredService<AbrirCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        if (HojaTactil.Mostrar(ventana) == true && ventana.DataContext is AbrirCajaViewModel vm)
            return vm.FondoInicial;
        return null;
    }

    public bool MostrarCorteCaja()
    {
        var ventana = _services.GetRequiredService<CorteCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public void MostrarBloqueo()
    {
        var ventana = _services.GetRequiredService<Features.Shell.BloqueoWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public void MostrarMovimientoCaja()
    {
        var ventana = _services.GetRequiredService<MovimientoCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public bool MostrarEditorCategoria(CategoriaDto? categoria)
    {
        var ventana = _services.GetRequiredService<CategoriaEditorWindow>();
        if (ventana.DataContext is CategoriaEditorViewModel vm) vm.Inicializar(categoria);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public void MostrarKardex(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<KardexWindow>();
        if (ventana.DataContext is KardexViewModel vm) vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public void MostrarHistorialPrecios(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<HistorialPrecioWindow>();
        if (ventana.DataContext is HistorialPrecioViewModel vm) vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public void MostrarVistaPreviaTicket(Guid ventaId)
    {
        var ventana = _services.GetRequiredService<TicketPreviewWindow>();
        if (ventana.DataContext is TicketPreviewViewModel vm) _ = vm.InicializarAsync(ventaId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public Application.Ventas.ResultadoCambioDto? MostrarCambioTalla(string? folio = null)
    {
        var ventana = _services.GetRequiredService<CambioTallaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        ventana.Precargar(folio);
        return HojaTactil.Mostrar(ventana) == true && ventana.DataContext is CambioTallaViewModel vm ? vm.Resultado : null;
    }

    public string? PedirTexto(string titulo, string indicacion)
    {
        var app = System.Windows.Application.Current;
        var caja = new System.Windows.Controls.TextBox
        {
            Style = (System.Windows.Style)app.FindResource("TextBox.Base"),
            Tag = indicacion,
            MinHeight = (double)app.FindResource("TouchTarget.Comodo")
        };
        var aceptar = new System.Windows.Controls.Button
        {
            Content = "Aceptar", IsDefault = true, MinWidth = 140, Margin = new System.Windows.Thickness(10, 0, 0, 0),
            Style = (System.Windows.Style)app.FindResource("Button.Primary")
        };
        var cancelar = new System.Windows.Controls.Button
        {
            Content = "Cancelar", IsCancel = true, MinWidth = 120,
            Style = (System.Windows.Style)app.FindResource("Button.Secondary")
        };
        var botones = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new System.Windows.Thickness(0, 16, 0, 0),
            Children = { cancelar, aceptar }
        };
        var ventana = new System.Windows.Window
        {
            Title = titulo, Width = 520, SizeToContent = System.Windows.SizeToContent.Height,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, ResizeMode = System.Windows.ResizeMode.NoResize,
            Owner = app.MainWindow,
            Background = (System.Windows.Media.Brush)app.FindResource("Brush.Bg.App"),
            Content = new System.Windows.Controls.StackPanel
            {
                Margin = new System.Windows.Thickness(24),
                Children =
                {
                    new System.Windows.Controls.TextBlock
                    {
                        Text = titulo, FontWeight = System.Windows.FontWeights.SemiBold,
                        FontSize = (double)app.FindResource("Font.Title"), Margin = new System.Windows.Thickness(0, 0, 0, 12)
                    },
                    caja, botones
                }
            }
        };
        aceptar.Click += (_, _) => ventana.DialogResult = true;
        ventana.Loaded += (_, _) => caja.Focus();
        return HojaTactil.Mostrar(ventana) == true && !string.IsNullOrWhiteSpace(caja.Text) ? caja.Text.Trim() : null;
    }

    public bool MostrarDevolucion(Guid ventaId, string folio)
    {
        var ventana = _services.GetRequiredService<DevolucionWindow>();
        if (ventana.DataContext is DevolucionViewModel vm) vm.Inicializar(ventaId, folio);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarMiCuenta()
    {
        var ventana = _services.GetRequiredService<MiCuentaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarAsistenteInicial()
    {
        var ventana = _services.GetRequiredService<Features.Configuracion.AsistenteInicialWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarAutorizacionSupervisor(string permiso, string accion)
    {
        var ventana = _services.GetRequiredService<AutorizacionSupervisorWindow>();
        if (ventana.DataContext is AutorizacionSupervisorViewModel vm)
            vm.Inicializar(permiso, accion);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public IReadOnlyList<(Application.Productos.Dtos.ProductoBusquedaDto Variante, int Cantidad)> MostrarMatrizVariantes(
        Application.Productos.Dtos.MatrizVariantesDto matriz, bool permitirAgotadas)
    {
        var ventana = _services.GetRequiredService<MatrizVariantesWindow>();
        ventana.Inicializar(matriz, permitirAgotadas);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true ? ventana.Seleccion : Array.Empty<(Application.Productos.Dtos.ProductoBusquedaDto, int)>();
    }

    public (bool Aceptado, ClienteDto? Cliente) SeleccionarCliente()
    {
        var ventana = _services.GetRequiredService<SelectorClienteWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true && ventana.DataContext is SelectorClienteViewModel vm
            ? (true, vm.Elegido)
            : (false, null);
    }

    public bool MostrarEditorCliente(ClienteDto? cliente)
    {
        var ventana = _services.GetRequiredService<ClienteEditorWindow>();
        if (ventana.DataContext is ClienteEditorViewModel vm) vm.Inicializar(cliente);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public void MostrarHistorialCliente(Guid clienteId, string nombre)
    {
        var ventana = _services.GetRequiredService<HistorialClienteWindow>();
        if (ventana.DataContext is HistorialClienteViewModel vm) vm.Inicializar(clienteId, nombre);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        HojaTactil.Mostrar(ventana);
    }

    public bool MostrarEstadoCuentaCliente(Guid clienteId, string nombre)
    {
        var ventana = _services.GetRequiredService<EstadoCuentaClienteWindow>();
        if (ventana.DataContext is EstadoCuentaClienteViewModel vm) vm.Inicializar(clienteId, nombre);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarEditorProveedor(ProveedorDto? proveedor)
    {
        var ventana = _services.GetRequiredService<ProveedorEditorWindow>();
        if (ventana.DataContext is ProveedorEditorViewModel vm) vm.Inicializar(proveedor);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarEditorCompra()
    {
        var ventana = _services.GetRequiredService<CompraEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarRecepcionCompra(Guid compraId)
    {
        var ventana = _services.GetRequiredService<RecepcionCompraWindow>();
        if (ventana.DataContext is RecepcionCompraViewModel vm) vm.Inicializar(compraId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarDevolucionProveedor(Guid compraId)
    {
        var ventana = _services.GetRequiredService<DevolucionProveedorWindow>();
        if (ventana.DataContext is DevolucionProveedorViewModel vm) vm.Inicializar(compraId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarPagosCompra(Guid compraId)
    {
        var ventana = _services.GetRequiredService<PagoCompraWindow>();
        if (ventana.DataContext is PagoCompraViewModel vm) vm.Inicializar(compraId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarEditorApartado()
    {
        var ventana = _services.GetRequiredService<ApartadoEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public ProductoBusquedaDto? SeleccionarProducto()
    {
        var ventana = _services.GetRequiredService<SelectorProductoWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true && ventana.DataContext is SelectorProductoViewModel vm
            ? vm.Seleccionado
            : null;
    }

    public bool MostrarAbono(Guid apartadoId, string folio, decimal saldo)
    {
        var ventana = _services.GetRequiredService<AbonoWindow>();
        if (ventana.DataContext is AbonoViewModel vm) vm.Inicializar(apartadoId, folio, saldo);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarEditorUsuario(UsuarioDto? usuario)
    {
        var ventana = _services.GetRequiredService<UsuarioEditorWindow>();
        if (ventana.DataContext is UsuarioEditorViewModel vm) vm.Inicializar(usuario);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public bool MostrarEditorRol(RolDetalleDto? rol)
    {
        var ventana = _services.GetRequiredService<Features.Roles.RolEditorWindow>();
        if (ventana.DataContext is Features.Roles.RolEditorViewModel vm) vm.Inicializar(rol);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public DescuentoResultado? MostrarDescuento(string contexto, TipoDescuento? tipoActual, decimal valorActual)
    {
        var ventana = _services.GetRequiredService<DescuentoWindow>();
        if (ventana.DataContext is DescuentoViewModel vm)
        {
            vm.Inicializar(contexto, tipoActual, valorActual);
            ventana.Owner = System.Windows.Application.Current.MainWindow;
            return HojaTactil.Mostrar(ventana) == true ? vm.Resultado : null;
        }
        return null;
    }

    public string? SeleccionarArchivoRespaldo()
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecciona el respaldo a restaurar",
            Filter = "Respaldo de base de datos (*.db)|*.db|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };
        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public string? SeleccionarCarpeta(string titulo)
    {
        var dialogo = new Microsoft.Win32.OpenFolderDialog { Title = titulo, Multiselect = false };
        return dialogo.ShowDialog() == true ? dialogo.FolderName : null;
    }

    public bool MostrarTomaFisica()
    {
        var ventana = _services.GetRequiredService<TomaFisicaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return HojaTactil.Mostrar(ventana) == true;
    }

    public string? SeleccionarArchivoCsv()
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecciona el archivo CSV a importar",
            Filter = "Archivo CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };
        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public string? SeleccionarImagen()
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecciona una imagen",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };
        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public string? GuardarComoCsv(string nombreSugerido)
    {
        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar a CSV",
            Filter = "Archivo CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
            FileName = nombreSugerido,
            DefaultExt = ".csv",
            AddExtension = true
        };
        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public string? GuardarComoArchivo(string nombreSugerido, string filtro, string extension)
    {
        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Guardar como",
            Filter = filtro,
            FileName = nombreSugerido,
            DefaultExt = extension,
            AddExtension = true
        };
        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    public void ReiniciarAplicacion()
    {
        var ruta = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(ruta))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = false });

        System.Windows.Application.Current.Shutdown();
    }

    public void Mensaje(string texto, string titulo = "USASHOPP POS") =>
        MostrarAviso(texto, titulo, "Entendido", null);

    public bool Confirmar(string texto, string titulo = "USASHOPP POS") =>
        MostrarAviso(texto, titulo, "Sí, continuar", "No") == true;

    /// <summary>
    /// Mensaje/confirmación con el diseño de la app y botones grandes (en lugar del MessageBox de
    /// Windows, con botones pequeños y fácil de tocar el equivocado).
    /// </summary>
    private static bool? MostrarAviso(string texto, string titulo, string aceptar, string? cancelar)
    {
        var app = System.Windows.Application.Current;
        if (app?.MainWindow is null || !app.MainWindow.IsLoaded)
        {
            // Antes de que exista la ventana principal (arranque/login) se usa el cuadro estándar.
            return cancelar is null
                ? MessageBox.Show(texto, titulo, MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK
                : MessageBox.Show(texto, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        T R<T>(string clave) => (T)app.FindResource(clave);
        var ok = new System.Windows.Controls.Button
        {
            Content = aceptar, IsDefault = true, MinWidth = 180, MinHeight = R<double>("TouchTarget.Grande"),
            Margin = new Thickness(10, 0, 0, 0), Style = R<Style>("Button.Primary")
        };
        var botones = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        };
        if (cancelar is not null)
            botones.Children.Add(new System.Windows.Controls.Button
            {
                Content = cancelar, IsCancel = true, MinWidth = 140, MinHeight = R<double>("TouchTarget.Grande"),
                Style = R<Style>("Button.Secondary")
            });
        botones.Children.Add(ok);

        var ventana = new Window
        {
            Title = titulo, Width = 560, SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
            Owner = app.MainWindow, Background = R<System.Windows.Media.Brush>("Brush.Bg.App"),
            Content = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(28, 20, 28, 24),
                Children =
                {
                    new System.Windows.Controls.TextBlock
                    {
                        Text = texto, TextWrapping = TextWrapping.Wrap, FontSize = R<double>("Font.Subtitle"),
                        LineHeight = 26
                    },
                    botones
                }
            }
        };
        ok.Click += (_, _) => ventana.DialogResult = true;
        return HojaTactil.Mostrar(ventana);
    }
}
