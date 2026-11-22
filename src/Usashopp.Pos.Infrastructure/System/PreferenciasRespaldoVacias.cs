using Usashopp.Pos.Application.Common.Interfaces.System;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>Valor por defecto si la UI no registra preferencias: sin carpeta externa.</summary>
public sealed class PreferenciasRespaldoVacias : IPreferenciasRespaldo
{
    public string? CarpetaRespaldoExterno => null;
}
