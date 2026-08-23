namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Hashing y verificación de contraseñas/PIN (nunca en texto plano).</summary>
public interface IPasswordHasher
{
    string Hash(string contrasena);
    bool Verificar(string contrasena, string hash);
}
