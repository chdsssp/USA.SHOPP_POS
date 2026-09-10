using Usashopp.Pos.Application.Reportes;

namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>Genera archivos (Excel/PDF) a partir de un reporte de negocio.</summary>
public interface IReporteExportador
{
    /// <summary>Libro de Excel (.xlsx) con el resumen y los desgloses del reporte.</summary>
    byte[] ExcelReporte(ReporteResumenDto reporte, DateTime? desde, DateTime? hasta);

    /// <summary>Documento PDF con el resumen y los desgloses del reporte.</summary>
    byte[] PdfReporte(ReporteResumenDto reporte, DateTime? desde, DateTime? hasta);
}
