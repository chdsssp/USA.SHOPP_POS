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
    public const string AuditoriaVer = "auditoria.ver";

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        VentasCrear, VentasCancelar, DescuentosAplicar, InventarioEditar,
        ComprasGestionar, ApartadosGestionar, ClientesGestionar, CajaCorte,
        ReportesVer, UsuariosGestionar, ConfigEditar, AuditoriaVer
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

    /// <summary>Etiquetas legibles de cada permiso, para la UI de roles.</summary>
    public static readonly IReadOnlyDictionary<string, string> Descripciones = new Dictionary<string, string>
    {
        [VentasCrear] = "Registrar ventas",
        [VentasCancelar] = "Cancelar ventas",
        [DescuentosAplicar] = "Aplicar descuentos y editar precios",
        [InventarioEditar] = "Editar inventario y catálogo",
        [ComprasGestionar] = "Gestionar compras y proveedores",
        [ApartadosGestionar] = "Gestionar apartados",
        [ClientesGestionar] = "Gestionar clientes",
        [CajaCorte] = "Corte de caja",
        [ReportesVer] = "Ver reportes y ventas",
        [UsuariosGestionar] = "Gestionar usuarios y roles",
        [ConfigEditar] = "Editar configuración",
        [AuditoriaVer] = "Ver bitácora de auditoría",
    };

    /// <summary>Etiqueta legible de un permiso (o su clave si no está catalogada).</summary>
    public static string Etiqueta(string clave) =>
        Descripciones.TryGetValue(clave, out var texto) ? texto : clave;
}
