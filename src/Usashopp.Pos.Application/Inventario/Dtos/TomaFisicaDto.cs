namespace Usashopp.Pos.Application.Inventario.Dtos;

/// <summary>Conteo físico de una variante (cantidad realmente contada).</summary>
public record TomaFisicaLineaDto(Guid VarianteId, int Conteo);

/// <summary>Resumen del resultado de aplicar una toma de inventario físico.</summary>
public record ResultadoTomaFisicaDto(int Ajustadas, int DiferenciaNeta);
