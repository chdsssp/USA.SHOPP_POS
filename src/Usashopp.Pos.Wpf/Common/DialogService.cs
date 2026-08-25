using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Inventario.Dtos;
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
        return ventana.ShowDialog() == true;
    }

    public bool MostrarAjusteStock(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<AjusteStockWindow>();
        if (ventana.DataContext is AjusteStockViewModel vm)
            vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public CobroResultado? MostrarCobro(decimal total)
    {
        var ventana = _services.GetRequiredService<CobroWindow>();
        if (ventana.DataContext is CobroViewModel vm)
        {
            vm.Inicializar(total);
            ventana.Owner = System.Windows.Application.Current.MainWindow;
            return ventana.ShowDialog() == true ? vm.Resultado : null;
        }
        return null;
    }

    public VentaEnEspera? MostrarVentasEnEspera()
    {
        var ventana = _services.GetRequiredService<VentasEnEsperaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true && ventana.DataContext is VentasEnEsperaViewModel vm
            ? vm.Seleccionada
            : null;
    }

    public decimal? MostrarAbrirCaja()
    {
        var ventana = _services.GetRequiredService<AbrirCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        if (ventana.ShowDialog() == true && ventana.DataContext is AbrirCajaViewModel vm)
            return vm.FondoInicial;
        return null;
    }

    public bool MostrarCorteCaja()
    {
        var ventana = _services.GetRequiredService<CorteCajaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorCategoria(CategoriaDto? categoria)
    {
        var ventana = _services.GetRequiredService<CategoriaEditorWindow>();
        if (ventana.DataContext is CategoriaEditorViewModel vm) vm.Inicializar(categoria);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public void MostrarKardex(VarianteInventarioDto variante)
    {
        var ventana = _services.GetRequiredService<KardexWindow>();
        if (ventana.DataContext is KardexViewModel vm) vm.Inicializar(variante);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        ventana.ShowDialog();
    }

    public void MostrarVistaPreviaTicket(Guid ventaId)
    {
        var ventana = _services.GetRequiredService<TicketPreviewWindow>();
        if (ventana.DataContext is TicketPreviewViewModel vm) _ = vm.InicializarAsync(ventaId);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        ventana.ShowDialog();
    }

    public bool MostrarDevolucion(Guid ventaId, string folio)
    {
        var ventana = _services.GetRequiredService<DevolucionWindow>();
        if (ventana.DataContext is DevolucionViewModel vm) vm.Inicializar(ventaId, folio);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarMiCuenta()
    {
        var ventana = _services.GetRequiredService<MiCuentaWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorCliente(ClienteDto? cliente)
    {
        var ventana = _services.GetRequiredService<ClienteEditorWindow>();
        if (ventana.DataContext is ClienteEditorViewModel vm) vm.Inicializar(cliente);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorProveedor(ProveedorDto? proveedor)
    {
        var ventana = _services.GetRequiredService<ProveedorEditorWindow>();
        if (ventana.DataContext is ProveedorEditorViewModel vm) vm.Inicializar(proveedor);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorCompra()
    {
        var ventana = _services.GetRequiredService<CompraEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorApartado()
    {
        var ventana = _services.GetRequiredService<ApartadoEditorWindow>();
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarAbono(Guid apartadoId, string folio, decimal saldo)
    {
        var ventana = _services.GetRequiredService<AbonoWindow>();
        if (ventana.DataContext is AbonoViewModel vm) vm.Inicializar(apartadoId, folio, saldo);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public bool MostrarEditorUsuario(UsuarioDto? usuario)
    {
        var ventana = _services.GetRequiredService<UsuarioEditorWindow>();
        if (ventana.DataContext is UsuarioEditorViewModel vm) vm.Inicializar(usuario);
        ventana.Owner = System.Windows.Application.Current.MainWindow;
        return ventana.ShowDialog() == true;
    }

    public DescuentoResultado? MostrarDescuento(string contexto, TipoDescuento? tipoActual, decimal valorActual)
    {
        var ventana = _services.GetRequiredService<DescuentoWindow>();
        if (ventana.DataContext is DescuentoViewModel vm)
        {
            vm.Inicializar(contexto, tipoActual, valorActual);
            ventana.Owner = System.Windows.Application.Current.MainWindow;
            return ventana.ShowDialog() == true ? vm.Resultado : null;
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

    public void ReiniciarAplicacion()
    {
        var ruta = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(ruta))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = false });

        System.Windows.Application.Current.Shutdown();
    }

    public void Mensaje(string texto, string titulo = "USASHOPP POS") =>
        MessageBox.Show(texto, titulo, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirmar(string texto, string titulo = "USASHOPP POS") =>
        MessageBox.Show(texto, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
