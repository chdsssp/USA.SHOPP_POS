namespace Usashopp.Pos.Application.Common.Models;

/// <summary>
/// Resultado de una operación que puede fallar por reglas de negocio esperadas,
/// evitando usar excepciones para el flujo normal.
/// </summary>
public class Result
{
    protected Result(bool exito, string? error)
    {
        Exito = exito;
        Error = error;
    }

    public bool Exito { get; }
    public bool EsFallo => !Exito;
    public string? Error { get; }

    public static Result Ok() => new(true, null);
    public static Result Falla(string error) => new(false, error);

    public static Result<T> Ok<T>(T valor) => new(valor, true, null);
    public static Result<T> Falla<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    internal Result(T? valor, bool exito, string? error) : base(exito, error) => Valor = valor;

    public T? Valor { get; }
}
