namespace Usashopp.Pos.Application.Common;

/// <summary>Catálogo de claves de permiso usadas en toda la app.</summary>
public static class Permisos
{
    public const string VentasCrear = "ventas.crear";
    public const string VentasCancelar = "ventas.cancelar";
    public const string DescuentosAplicar = "descuentos.aplicar";
    public const string InventarioEditar = "inventario.editar";
    public const string ComprasGestionar = "compras.gestionar";
    public const string ApartadosGestionar = "apartados.gestionar";
    public const string ClientesGestionar = "clientes.gestionar";
    public const string CajaCorte = "caja.corte";
    public const string ReportesVer = "reportes.ver";
    public const string UsuariosGestionar = "usuarios.gestionar";
    public const string ConfigEditar = "config.editar";

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        VentasCrear, VentasCancelar, DescuentosAplicar, InventarioEditar,
        ComprasGestionar, ApartadosGestionar, ClientesGestionar, CajaCorte,
        ReportesVer, UsuariosGestionar, ConfigEditar
    };

    /// <summary>Permisos base del rol Cajero.</summary>
    public static readonly IReadOnlyList<string> Cajero = new[]
    {
        VentasCrear, ApartadosGestionar, ClientesGestionar
    };

    /// <summary>Permisos del rol Encargado.</summary>
    public static readonly IReadOnlyList<string> Encargado = new[]
    {
        VentasCrear, VentasCancelar, DescuentosAplicar, ApartadosGestionar,
        ClientesGestionar, CajaCorte, ReportesVer
    };
}
