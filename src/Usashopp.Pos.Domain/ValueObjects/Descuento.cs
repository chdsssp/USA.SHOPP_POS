using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Domain.ValueObjects;

/// <summary>
/// Descuento aplicable a una línea o a una venta completa. Sabe calcular el importe
/// que descuenta sobre un <see cref="Dinero"/> base.
/// </summary>
public readonly record struct Descuento
{
    public TipoDescuento Tipo { get; }
    public decimal Valor { get; }

    public Descuento(TipoDescuento tipo, decimal valor)
    {
        if (valor < 0)
            throw new ArgumentException("El descuento no puede ser negativo.", nameof(valor));
        if (tipo == TipoDescuento.Porcentaje && valor > 100)
            throw new ArgumentException("Un descuento porcentual no puede superar 100%.", nameof(valor));

        Tipo = tipo;
        Valor = valor;
    }

    public static Descuento Porcentaje(decimal porcentaje) => new(TipoDescuento.Porcentaje, porcentaje);
    public static Descuento Monto(decimal monto) => new(TipoDescuento.MontoFijo, monto);

    /// <summary>Importe que descuenta sobre <paramref name="baseImporte"/> (nunca mayor que la base).</summary>
    public Dinero CalcularSobre(Dinero baseImporte)
    {
        var descuento = Tipo switch
        {
            TipoDescuento.Porcentaje => baseImporte.Monto * (Valor / 100m),
            TipoDescuento.MontoFijo => Valor,
            _ => 0m
        };

        return new Dinero(Math.Min(descuento, baseImporte.Monto), baseImporte.Moneda);
    }
}
