using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Wpf.Features.Pos;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>Abre ventanas modales desde los ViewModels (sin acoplarlos a WPF).</summary>
public interface IDialogService
{
    /// <summary>Editor de alta de producto. Devuelve true si se guardó.</summary>
    bool MostrarEditorProducto();

    /// <summary>Diálogo de ajuste de stock de una variante. Devuelve true si se ajustó.</summary>
    bool MostrarAjusteStock(VarianteInventarioDto variante);

    /// <summary>Diálogo de cobro. Devuelve los pagos y el cambio, o null si se canceló.</summary>
    CobroResultado? MostrarCobro(decimal total);

    /// <summary>Diálogo de apertura de caja. Devuelve el fondo inicial, o null si se canceló.</summary>
    decimal? MostrarAbrirCaja();

    /// <summary>Diálogo de corte de caja. Devuelve true si se cerró la caja.</summary>
    bool MostrarCorteCaja();

    /// <summary>Editor de cliente (null = nuevo). Devuelve true si se guardó.</summary>
    bool MostrarEditorCliente(ClienteDto? cliente);

    /// <summary>Editor de proveedor (null = nuevo). Devuelve true si se guardó.</summary>
    bool MostrarEditorProveedor(ProveedorDto? proveedor);

    /// <summary>Editor de alta de compra. Devuelve true si se registró.</summary>
    bool MostrarEditorCompra();

    /// <summary>Editor de alta de apartado. Devuelve true si se creó.</summary>
    bool MostrarEditorApartado();

    /// <summary>Diálogo de abono a un apartado. Devuelve true si se registró.</summary>
    bool MostrarAbono(Guid apartadoId, string folio, decimal saldo);

    /// <summary>Editor de usuario (null = nuevo). Devuelve true si se guardó.</summary>
    bool MostrarEditorUsuario(UsuarioDto? usuario);

    /// <summary>Mensaje simple de información/error.</summary>
    void Mensaje(string texto, string titulo = "USASHOPP POS");
}
