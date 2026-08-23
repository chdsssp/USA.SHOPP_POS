using System.Security.Cryptography;
using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Hashing de contraseñas con PBKDF2 (SHA-256, sal aleatoria por contraseña).
/// Formato almacenado: "iteraciones.salBase64.hashBase64".
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public string Hash(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string hashAlmacenado)
    {
        var partes = hashAlmacenado.Split('.', 3);
        if (partes.Length != 3) return false;

        var iteraciones = int.Parse(partes[0]);
        var sal = Convert.FromBase64String(partes[1]);
        var hashEsperado = Convert.FromBase64String(partes[2]);

        var hashActual = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
        return CryptographicOperations.FixedTimeEquals(hashActual, hashEsperado);
    }
}
