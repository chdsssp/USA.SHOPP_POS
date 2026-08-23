using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>Contenido temporal para secciones aún no implementadas (Fase 1).</summary>
public class PlaceholderViewModel : ViewModelBase
{
    public string Titulo { get; }
    public string Descripcion { get; }

    public PlaceholderViewModel(string titulo)
    {
        Titulo = titulo;
        Descripcion = "Esta sección se construirá en las próximas fases del roadmap.";
    }
}
