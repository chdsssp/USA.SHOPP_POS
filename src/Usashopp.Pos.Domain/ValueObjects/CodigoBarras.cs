namespace Usashopp.Pos.Domain.ValueObjects;

/// <summary>
/// Código de barras de una variante (EAN-13 / UPC-A / Code128, según se use en tienda).
/// </summary>
public readonly record struct CodigoBarras
{
    public string Valor { get; }

    public CodigoBarras(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El código de barras no puede estar vacío.", nameof(valor));

        Valor = valor.Trim();
    }

    public override string ToString() => Valor;

    public static implicit operator string(CodigoBarras codigo) => codigo.Valor;
}
