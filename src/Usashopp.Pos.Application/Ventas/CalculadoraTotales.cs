using Usashopp.Pos.Application.Promociones;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.Services;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Ventas;

/// <summary>Una línea del carrito tal como la captura el POS.</summary>
public record LineaCalculo(
    Guid VarianteId,
    int Cantidad,
    decimal Precio,
    TipoDescuento? DescuentoTipo = null,
    decimal DescuentoValor = 0,
    Guid? ProductoId = null,
    Guid? CategoriaId = null);

/// <summary>Resultado por línea: bruto (precio × cantidad), descuento total (manual + promoción) e importe final.</summary>
public record LineaCalculada(decimal Bruto, decimal Descuento, decimal Importe, decimal DescuentoPromocion = 0, string? Promocion = null);

/// <summary>Totales del carrito calculados con las mismas reglas del dominio.</summary>
public record TotalesCalculados(
    IReadOnlyList<LineaCalculada> Lineas,
    decimal Subtotal,
    decimal DescuentoGlobal,
    decimal Total,
    decimal Impuesto,
    int Articulos);

/// <summary>
/// Única fuente de verdad para los totales de una venta. Construye las mismas entidades del
/// dominio (<see cref="Venta"/>/<see cref="DetalleVenta"/>) y aplica el mismo motor de promociones
/// (<see cref="MotorPromociones"/>) que usa <see cref="RegistrarVentaService"/>: lo que ve el cajero
/// en pantalla es exactamente lo que se guarda y se cobra.
/// </summary>
public static class CalculadoraTotales
{
    public static TotalesCalculados Calcular(
        IReadOnlyList<LineaCalculo> lineas,
        TipoDescuento? descuentoGlobalTipo = null,
        decimal descuentoGlobalValor = 0,
        decimal tasaImpuesto = 0.16m,
        bool impuestoIncluido = true,
        IReadOnlyList<PromocionDto>? promociones = null,
        DateTime? fechaLocal = null)
    {
        var venta = new Venta();
        var detalles = new List<DetalleVenta>(lineas.Count);

        foreach (var l in lineas)
        {
            var detalle = new DetalleVenta
            {
                VarianteId = l.VarianteId,
                Cantidad = Math.Max(1, l.Cantidad),
                PrecioUnitario = new Dinero(Math.Max(0, l.Precio)),
                Descuento = CrearDescuento(l.DescuentoTipo, l.DescuentoValor)
            };
            // Se agrega directo (sin fusionar líneas) para conservar el orden 1:1 con el carrito.
            venta.Detalles.Add(detalle);
            detalles.Add(detalle);
        }

        if (promociones is { Count: > 0 })
            AplicarPromociones(detalles, i => (lineas[i].ProductoId, lineas[i].CategoriaId),
                promociones.Select(p => p.ComoEntidad()).ToList(), fechaLocal ?? DateTime.Now);

        venta.DescuentoGlobal = CrearDescuento(descuentoGlobalTipo, descuentoGlobalValor);

        var calculadas = detalles.Select(d =>
        {
            var bruto = d.PrecioUnitario.Por(d.Cantidad).Monto;
            return new LineaCalculada(bruto, bruto - d.Importe.Monto, d.Importe.Monto, d.DescuentoPromocion.Monto, d.PromocionNombre);
        }).ToList();

        var total = venta.Total;
        var impuesto = CalculadoraImpuestos.Desglosar(total, tasaImpuesto, impuestoIncluido).Impuesto.Monto;

        return new TotalesCalculados(
            calculadas,
            venta.Subtotal.Monto,
            venta.TotalDescuentoGlobal.Monto,
            total.Monto,
            impuesto,
            venta.Detalles.Sum(d => d.Cantidad));
    }

    /// <summary>
    /// Corre el motor de promociones sobre las líneas y deja el resultado en cada <see cref="DetalleVenta"/>
    /// (descuento, id y nombre congelado). Lo usan el carrito y el registro de la venta.
    /// </summary>
    internal static void AplicarPromociones(IReadOnlyList<DetalleVenta> detalles,
        Func<int, (Guid? ProductoId, Guid? CategoriaId)> clasificacion, IReadOnlyList<Promocion> promociones, DateTime fechaLocal)
    {
        var entrada = detalles.Select((d, i) =>
        {
            var (prod, cat) = clasificacion(i);
            return new LineaPromo(i, d.VarianteId, prod, cat, d.Cantidad, d.PrecioUnitario.Monto,
                d.Descuento is { Valor: > 0 });
        }).ToList();

        foreach (var d in detalles)
        {
            d.DescuentoPromocion = Dinero.Cero;
            d.PromocionId = null;
            d.PromocionNombre = null;
        }

        foreach (var a in MotorPromociones.Aplicar(entrada, promociones, fechaLocal))
        {
            var d = detalles[a.Indice];
            // El descuento de la promoción se calcula sobre el precio de lista; si además hay manual
            // acumulable, nunca deja la línea en negativo (lo acota DetalleVenta.Importe).
            d.DescuentoPromocion = new Dinero(a.Descuento);
            d.PromocionId = a.PromocionId;
            d.PromocionNombre = a.Nombre;
        }
    }

    /// <summary>Descuento válido para el dominio (acota % a 0–100 y montos a ≥ 0); null si no aplica.</summary>
    public static Descuento? CrearDescuento(TipoDescuento? tipo, decimal valor)
    {
        if (tipo is not { } t || valor <= 0) return null;
        if (t == TipoDescuento.Porcentaje) valor = Math.Min(valor, 100m);
        return new Descuento(t, valor);
    }
}
