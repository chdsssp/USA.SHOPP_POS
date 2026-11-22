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
    decimal DescuentoValor = 0);

/// <summary>Resultado por línea: bruto (precio × cantidad), descuento e importe final.</summary>
public record LineaCalculada(decimal Bruto, decimal Descuento, decimal Importe);

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
/// dominio (<see cref="Venta"/>/<see cref="DetalleVenta"/>) que usa <see cref="RegistrarVentaService"/>,
/// así lo que ve el cajero en pantalla es exactamente lo que se guarda y se cobra.
/// </summary>
public static class CalculadoraTotales
{
    public static TotalesCalculados Calcular(
        IReadOnlyList<LineaCalculo> lineas,
        TipoDescuento? descuentoGlobalTipo = null,
        decimal descuentoGlobalValor = 0,
        decimal tasaImpuesto = 0.16m,
        bool impuestoIncluido = true)
    {
        var venta = new Venta();
        var calculadas = new List<LineaCalculada>(lineas.Count);

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

            var bruto = detalle.PrecioUnitario.Por(detalle.Cantidad).Monto;
            calculadas.Add(new LineaCalculada(bruto, bruto - detalle.Importe.Monto, detalle.Importe.Monto));
        }

        venta.DescuentoGlobal = CrearDescuento(descuentoGlobalTipo, descuentoGlobalValor);

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

    /// <summary>Descuento válido para el dominio (acota % a 0–100 y montos a ≥ 0); null si no aplica.</summary>
    public static Descuento? CrearDescuento(TipoDescuento? tipo, decimal valor)
    {
        if (tipo is not { } t || valor <= 0) return null;
        if (t == TipoDescuento.Porcentaje) valor = Math.Min(valor, 100m);
        return new Descuento(t, valor);
    }
}
