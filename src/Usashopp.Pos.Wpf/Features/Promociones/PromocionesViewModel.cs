using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Catalogo;
using Usashopp.Pos.Application.Promociones;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Promociones;

/// <summary>Opción de categoría para el alcance de una promoción.</summary>
public record OpcionCategoria(Guid Id, string Nombre);

/// <summary>
/// Promociones automáticas (2×1, 3×$299, rebajas de temporada, precio especial): lista y editor en
/// la misma pantalla (panel a la derecha), pensado para el dedo.
/// </summary>
public partial class PromocionesViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;

    public ObservableCollection<PromocionDto> Promociones { get; } = new();
    public ObservableCollection<OpcionCategoria> Categorias { get; } = new();

    [ObservableProperty] private PromocionDto? _seleccionada;
    [ObservableProperty] private bool _editando;
    [ObservableProperty] private string? _error;

    // ---- Campos del editor ----
    private Guid _id;
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsaValor), nameof(UsaN), nameof(UsaM), nameof(EtiquetaValor))]
    private TipoPromocion _tipo = TipoPromocion.Porcentaje;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EligeCategoria), nameof(EligeProductoOVariante))]
    private AlcancePromocion _alcance = AlcancePromocion.Todo;
    [ObservableProperty] private Guid? _alcanceId;
    [ObservableProperty] private string? _alcanceTexto;
    [ObservableProperty] private OpcionCategoria? _categoria;
    [ObservableProperty] private decimal _valor;
    [ObservableProperty] private int _n = 2;
    [ObservableProperty] private int _m = 1;
    [ObservableProperty] private DateTime? _inicio;
    [ObservableProperty] private DateTime? _fin;
    [ObservableProperty] private bool _activa = true;
    [ObservableProperty] private int _prioridad;
    [ObservableProperty] private bool _acumulable;

    public bool UsaValor => Tipo != TipoPromocion.NxM;
    public bool UsaN => Tipo is TipoPromocion.NxM or TipoPromocion.NPorPrecio;
    public bool UsaM => Tipo == TipoPromocion.NxM;
    public bool EligeCategoria => Alcance == AlcancePromocion.Categoria;
    public bool EligeProductoOVariante => Alcance is AlcancePromocion.Producto or AlcancePromocion.Variante;
    public string EtiquetaValor => Tipo switch
    {
        TipoPromocion.Porcentaje => "Porcentaje de descuento (%)",
        TipoPromocion.MontoPorPieza => "Monto que se descuenta por pieza ($)",
        TipoPromocion.NPorPrecio => "Precio del paquete ($)",
        TipoPromocion.PrecioEspecial => "Precio especial por pieza ($)",
        _ => "Valor"
    };

    public PromocionesViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = CargarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var lista = await scope.ServiceProvider.GetRequiredService<PromocionService>().ListarAsync();
        Promociones.Clear();
        foreach (var p in lista) Promociones.Add(p);

        var cats = await scope.ServiceProvider.GetRequiredService<CategoriaService>().ListarAsync();
        Categorias.Clear();
        foreach (var c in cats) Categorias.Add(new OpcionCategoria(c.Id, c.Nombre));
    }

    partial void OnCategoriaChanged(OpcionCategoria? value)
    {
        if (Alcance == AlcancePromocion.Categoria) AlcanceId = value?.Id;
    }

    [RelayCommand]
    private void Nueva()
    {
        _id = Guid.Empty;
        Nombre = string.Empty;
        Tipo = TipoPromocion.Porcentaje;
        Alcance = AlcancePromocion.Todo;
        AlcanceId = null;
        AlcanceTexto = null;
        Categoria = null;
        Valor = 10;
        N = 2;
        M = 1;
        Inicio = DateTime.Today;
        Fin = null;
        Activa = true;
        Prioridad = 0;
        Acumulable = false;
        Error = null;
        Editando = true;
    }

    [RelayCommand]
    private void Editar(PromocionDto? p)
    {
        p ??= Seleccionada;
        if (p is null) return;
        _id = p.Id;
        Nombre = p.Nombre;
        Tipo = p.Tipo;
        Alcance = p.Alcance;
        AlcanceId = p.AlcanceId;
        AlcanceTexto = p.AlcanceNombre;
        Categoria = Categorias.FirstOrDefault(c => c.Id == p.AlcanceId);
        Valor = p.Valor;
        N = p.N;
        M = p.M;
        Inicio = p.Inicio;
        Fin = p.Fin;
        Activa = p.Activa;
        Prioridad = p.Prioridad;
        Acumulable = p.AcumulableConManual;
        Error = null;
        Editando = true;
    }

    /// <summary>Elige el producto (o la variante exacta) al que aplica la promoción.</summary>
    [RelayCommand]
    private void ElegirArticulo()
    {
        var elegido = _dialogos.SeleccionarProducto();
        if (elegido is null) return;
        if (Alcance == AlcancePromocion.Producto)
        {
            AlcanceId = elegido.ProductoId;
            AlcanceTexto = "Producto: " + elegido.Descripcion.Split('—')[0].Trim();
        }
        else
        {
            AlcanceId = elegido.VarianteId;
            AlcanceTexto = "Variante: " + elegido.Descripcion;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        if (Alcance == AlcancePromocion.Categoria) AlcanceId = Categoria?.Id;
        var dto = new PromocionDto(_id, Nombre, Tipo, Alcance, AlcanceId, Valor, N, M, Inicio, Fin, 0,
            Activa, Prioridad, Acumulable);
        using var scope = _scopeFactory.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<PromocionService>().GuardarAsync(dto);
        if (r.EsFallo) { Error = r.Error; return; }
        Editando = false;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (_id == Guid.Empty) { Editando = false; return; }
        if (!_dialogos.Confirmar($"¿Eliminar la promoción «{Nombre}»?", "Eliminar promoción")) return;
        using var scope = _scopeFactory.CreateScope();
        var r = await scope.ServiceProvider.GetRequiredService<PromocionService>().EliminarAsync(_id);
        if (r.EsFallo) { Error = r.Error; return; }
        Editando = false;
        await CargarAsync();
    }

    [RelayCommand]
    private void CancelarEdicion() => Editando = false;
}
