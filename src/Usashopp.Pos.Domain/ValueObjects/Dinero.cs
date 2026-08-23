using System.Globalization;

namespace Usashopp.Pos.Domain.ValueObjects;

/// <summary>
/// Importe monetario. Encapsula un <see cref="decimal"/> (nunca double) y su moneda
/// para evitar errores de redondeo y mezcla de monedas. La tienda opera en MXN.
/// </summary>
public readonly record struct Dinero
{
    public const string MonedaPredeterminada = "MXN";

    public decimal Monto { get; }
    public string Moneda { get; }

    public Dinero(decimal monto, string moneda = MonedaPredeterminada)
    {
        if (string.IsNullOrWhiteSpace(moneda))
            throw new ArgumentException("La moneda es obligatoria.", nameof(moneda));

        // Redondeo bancario a 2 decimales para consistencia en toda la app.
        Monto = Math.Round(monto, 2, MidpointRounding.ToEven);
        Moneda = moneda.ToUpperInvariant();
    }

    public static Dinero Cero => new(0m);

    public static Dinero De(decimal monto) => new(monto);

    public Dinero Mas(Dinero otro)
    {
        ValidarMisma(otro);
        return new Dinero(Monto + otro.Monto, Moneda);
    }

    public Dinero Menos(Dinero otro)
    {
        ValidarMisma(otro);
        return new Dinero(Monto - otro.Monto, Moneda);
    }

    public Dinero Por(decimal cantidad) => new(Monto * cantidad, Moneda);

    public bool EsNegativo => Monto < 0;
    public bool EsCero => Monto == 0;

    private void ValidarMisma(Dinero otro)
    {
        if (Moneda != otro.Moneda)
            throw new InvalidOperationException($"No se pueden operar montos en monedas distintas ({Moneda} vs {otro.Moneda}).");
    }

    public override string ToString() =>
        Monto.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
}
