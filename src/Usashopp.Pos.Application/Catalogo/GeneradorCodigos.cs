using System.Text;

namespace Usashopp.Pos.Application.Catalogo;

/// <summary>Genera SKU internos y códigos de barras EAN-13 para variantes nuevas.</summary>
public static class GeneradorCodigos
{
    // Base32 sin caracteres ambiguos (sin I, O) para SKU legibles.
    private const string Alfabeto = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>SKU aleatorio con prefijo "SKU" y 8 caracteres base32 (p. ej. "SKU7K2M9QA4").</summary>
    public static string NuevoSku()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        var sb = new StringBuilder("SKU", 11);
        for (var i = 0; i < 8; i++)
            sb.Append(Alfabeto[bytes[i] % Alfabeto.Length]);
        return sb.ToString();
    }

    /// <summary>
    /// Código de barras EAN-13 de uso interno. Usa el prefijo 200 (rango reservado para
    /// numeración interna de la tienda) + 9 dígitos aleatorios + dígito de control.
    /// </summary>
    public static string NuevoCodigoBarras()
    {
        var d = new int[13];
        d[0] = 2; d[1] = 0; d[2] = 0;
        for (var i = 3; i < 12; i++) d[i] = Random.Shared.Next(10);
        d[12] = DigitoControlEan13(d);
        return string.Concat(d);
    }

    private static int DigitoControlEan13(int[] d)
    {
        var suma = 0;
        for (var i = 0; i < 12; i++)
            suma += d[i] * (i % 2 == 0 ? 1 : 3);
        return (10 - suma % 10) % 10;
    }
}
