using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Application.Promociones;

/// <summary>Promoción tal como la ve la UI (y la calculadora del carrito).</summary>
public record PromocionDto(
    Guid Id,
    string Nombre,
    TipoPromocion Tipo,
    AlcancePromocion Alcance,
    Guid? AlcanceId,
    decimal Valor,
    int N,
    int M,
    DateTime? Inicio,
    DateTime? Fin,
    int DiasSemana,
    bool Activa,
    int Prioridad,
    bool AcumulableConManual,
    string? AlcanceNombre = null)
{
    public string Descripcion => Tipo switch
    {
        TipoPromocion.Porcentaje => $"{Valor:0.##}% de descuento",
        TipoPromocion.MontoPorPieza => $"{Valor:C2} menos por pieza",
        TipoPromocion.NxM => $"{N}×{M}",
        TipoPromocion.NPorPrecio => $"{N} por {Valor:C2}",
        TipoPromocion.PrecioEspecial => $"Precio especial {Valor:C2}",
        _ => Tipo.ToString()
    };

    public string VigenciaTexto => (Inicio, Fin) switch
    {
        (null, null) => "Sin fecha límite",
        ({ } i, null) => $"Desde {i:dd/MM/yyyy}",
        (null, { } f) => $"Hasta {f:dd/MM/yyyy}",
        ({ } i, { } f) => $"{i:dd/MM} – {f:dd/MM/yyyy}"
    };

    public string AlcanceTexto => Alcance switch
    {
        AlcancePromocion.Todo => "Toda la tienda",
        _ => AlcanceNombre ?? Alcance.ToString()
    };

    internal Promocion ComoEntidad()
    {
        var p = new Promocion
        {
            Nombre = Nombre, Tipo = Tipo, Alcance = Alcance, AlcanceId = AlcanceId, Valor = Valor, N = N, M = M,
            Inicio = Inicio, Fin = Fin, DiasSemana = DiasSemana, Activa = Activa, Prioridad = Prioridad,
            AcumulableConManual = AcumulableConManual
        };
        IdentidadPromocion.Asignar(p, Id);
        return p;
    }
}

/// <summary>Alta, edición y consulta de promociones (permiso promociones.gestionar).</summary>
public class PromocionService
{
    private readonly IRepository<Promocion> _promociones;
    private readonly IRepository<Categoria> _categorias;
    private readonly IRepository<Producto> _productos;
    private readonly IVarianteRepository _variantes;
    private readonly ICurrentUser _usuario;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;
    private readonly IDateTime _reloj;

    public PromocionService(IRepository<Promocion> promociones, IRepository<Categoria> categorias, IRepository<Producto> productos,
        IVarianteRepository variantes, ICurrentUser usuario, IUnitOfWork uow, IAuditoria auditoria, IDateTime reloj)
    {
        _promociones = promociones;
        _categorias = categorias;
        _productos = productos;
        _variantes = variantes;
        _usuario = usuario;
        _uow = uow;
        _auditoria = auditoria;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<PromocionDto>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _promociones.ListarAsync(null, ct);
        var resultado = new List<PromocionDto>();
        foreach (var p in lista.OrderByDescending(p => p.Activa).ThenByDescending(p => p.Prioridad).ThenBy(p => p.Nombre))
            resultado.Add(Map(p, await NombreAlcanceAsync(p, ct)));
        return resultado;
    }

    /// <summary>Promociones vigentes HOY (para el carrito del POS).</summary>
    public async Task<IReadOnlyList<PromocionDto>> ListarVigentesAsync(CancellationToken ct = default)
    {
        var hoy = Fechas.UtcALocal(_reloj.UtcAhora);
        var lista = await _promociones.ListarAsync(p => p.Activa, ct);
        return lista.Where(p => p.VigenteEn(hoy)).Select(p => Map(p, null)).ToList();
    }

    public async Task<Result> GuardarAsync(PromocionDto dto, CancellationToken ct = default)
    {
        if (!_usuario.TienePermiso(Permisos.PromocionesGestionar))
            return Result.Falla("No tienes permiso para gestionar promociones.");
        var error = Validar(dto);
        if (error is not null) return Result.Falla(error);

        var p = dto.Id == Guid.Empty ? null : await _promociones.ObtenerPorIdAsync(dto.Id, ct);
        var nueva = p is null;
        p ??= new Promocion();
        p.Nombre = dto.Nombre.Trim();
        p.Tipo = dto.Tipo;
        p.Alcance = dto.Alcance;
        p.AlcanceId = dto.Alcance == AlcancePromocion.Todo ? null : dto.AlcanceId;
        p.Valor = dto.Valor;
        p.N = dto.N;
        p.M = dto.M;
        p.Inicio = dto.Inicio?.Date;
        p.Fin = dto.Fin?.Date;
        p.DiasSemana = dto.DiasSemana;
        p.Activa = dto.Activa;
        p.Prioridad = dto.Prioridad;
        p.AcumulableConManual = dto.AcumulableConManual;

        if (nueva) await _promociones.AgregarAsync(p, ct);
        else _promociones.Actualizar(p);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync(nueva ? "Alta de promoción" : "Edición de promoción", $"«{p.Nombre}» {Map(p, null).Descripcion}", "Promocion", p.Id, ct);
        return Result.Ok();
    }

    public async Task<Result> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (!_usuario.TienePermiso(Permisos.PromocionesGestionar))
            return Result.Falla("No tienes permiso para gestionar promociones.");
        var p = await _promociones.ObtenerPorIdAsync(id, ct);
        if (p is null) return Result.Ok();
        _promociones.Eliminar(p);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Baja de promoción", $"«{p.Nombre}»", "Promocion", p.Id, ct);
        return Result.Ok();
    }

    internal static string? Validar(PromocionDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Nombre)) return "Escribe un nombre para la promoción.";
        if (d.Alcance != AlcancePromocion.Todo && d.AlcanceId is null) return "Elige a qué categoría, producto o variante aplica.";
        if (d.Inicio is { } i && d.Fin is { } f && f.Date < i.Date) return "La fecha final es anterior a la inicial.";
        return d.Tipo switch
        {
            TipoPromocion.Porcentaje when d.Valor is <= 0 or > 100 => "El porcentaje debe estar entre 1 y 100.",
            TipoPromocion.MontoPorPieza or TipoPromocion.PrecioEspecial when d.Valor <= 0 => "Indica un monto mayor que cero.",
            TipoPromocion.NxM when d.N < 2 || d.M < 1 || d.M >= d.N => "En N×M, N debe ser mayor que M (p. ej. 2×1, 3×2).",
            TipoPromocion.NPorPrecio when d.N < 2 || d.Valor <= 0 => "Indica cuántas piezas (2 o más) y el precio del paquete.",
            _ => null
        };
    }

    private async Task<string?> NombreAlcanceAsync(Promocion p, CancellationToken ct)
    {
        if (p.AlcanceId is not { } id) return null;
        return p.Alcance switch
        {
            AlcancePromocion.Categoria => "Categoría: " + (await _categorias.ObtenerPorIdAsync(id, ct))?.Nombre,
            AlcancePromocion.Producto => "Producto: " + (await _productos.ObtenerPorIdAsync(id, ct))?.Nombre,
            AlcancePromocion.Variante => "Variante: " + (await _variantes.ObtenerPorIdAsync(id, ct))?.DescripcionCompleta,
            _ => null
        };
    }

    private static PromocionDto Map(Promocion p, string? alcance) => new(
        p.Id, p.Nombre, p.Tipo, p.Alcance, p.AlcanceId, p.Valor, p.N, p.M, p.Inicio, p.Fin, p.DiasSemana,
        p.Activa, p.Prioridad, p.AcumulableConManual, alcance);
}

/// <summary>El Id de EntidadBase es de solo lectura fuera del dominio; el DTO necesita conservarlo para el motor.</summary>
internal static class IdentidadPromocion
{
    private static readonly System.Reflection.PropertyInfo Prop =
        typeof(Domain.Common.EntidadBase).GetProperty(nameof(Domain.Common.EntidadBase.Id))!;

    public static void Asignar(Promocion p, Guid id)
    {
        if (id != Guid.Empty) Prop.SetValue(p, id);
    }
}
