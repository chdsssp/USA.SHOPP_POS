namespace Usashopp.Pos.Domain.ValueObjects;

/// <summary>
/// Identificador interno de una variante de producto. Se normaliza a mayúsculas sin espacios.
/// </summary>
public readonly record struct Sku
{
    public string Valor { get; }

    public Sku(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El SKU no puede estar vacío.", nameof(valor));

        Valor = valor.Trim().ToUpperInvariant();
    }

    public override string ToString() => Valor;

    public static implicit operator string(Sku sku) => sku.Valor;
}
