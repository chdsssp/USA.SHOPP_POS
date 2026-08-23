using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Infrastructure.Persistence.Converters;

/// <summary>Dinero se almacena como decimal (moneda fija MXN en esta tienda).</summary>
public sealed class DineroConverter : ValueConverter<Dinero, decimal>
{
    public DineroConverter() : base(d => d.Monto, m => new Dinero(m)) { }
}

public sealed class SkuConverter : ValueConverter<Sku, string>
{
    public SkuConverter() : base(s => s.Valor, v => new Sku(v)) { }
}

public sealed class CodigoBarrasConverter : ValueConverter<CodigoBarras, string>
{
    public CodigoBarrasConverter() : base(c => c.Valor, v => new CodigoBarras(v)) { }
}

/// <summary>Descuento se serializa como "P|valor" (porcentaje) o "M|valor" (monto fijo).</summary>
public sealed class DescuentoConverter : ValueConverter<Descuento, string>
{
    public DescuentoConverter() : base(d => Serializar(d), s => Deserializar(s)) { }

    public static string Serializar(Descuento d) =>
        $"{(d.Tipo == TipoDescuento.Porcentaje ? "P" : "M")}|{d.Valor}";

    public static Descuento Deserializar(string s)
    {
        var partes = s.Split('|');
        var tipo = partes[0] == "P" ? TipoDescuento.Porcentaje : TipoDescuento.MontoFijo;
        var valor = decimal.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture);
        return new Descuento(tipo, valor);
    }
}
