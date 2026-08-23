using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Apartados;
using Usashopp.Pos.Application.Apartados.Dtos;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Productos;
using Usashopp.Pos.Application.Productos.Dtos;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Apartados;

/// <summary>Alta de un apartado: cliente, líneas y anticipo inicial.</summary>
public partial class ApartadoEditorViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private ClienteDto? _clienteSeleccionado;
    [ObservableProperty] private ProductoBusquedaDto? _varianteSeleccionada;
    [ObservableProperty] private int _cantidad = 1;
    [ObservableProperty] private decimal _precio;
    [ObservableProperty] private decimal _anticipoInicial;
    [ObservableProperty] private MetodoPago _metodoAnticipo = MetodoPago.Efectivo;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _guardando;

    public ObservableCollection<ClienteDto> Clientes { get; } = new();
    public ObservableCollection<ProductoBusquedaDto> Variantes { get; } = new();
    public ObservableCollection<LineaApartadoEditable> Lineas { get; } = new();
    public MetodoPago[] Metodos { get; } = { MetodoPago.Efectivo, MetodoPago.Tarjeta, MetodoPago.Transferencia };

    public decimal Total => Lineas.Sum(l => l.Importe);

    public event Action<bool>? Cerrar;

    public ApartadoEditorViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ = CargarAsync();
    }

    partial void OnVarianteSeleccionadaChanged(ProductoBusquedaDto? value)
    {
        if (value is not null) Precio = value.Precio;
    }

    private async Task CargarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var clientes = await scope.ServiceProvider.GetRequiredService<ClienteService>().ListarAsync();
        var variantes = await scope.ServiceProvider.GetRequiredService<BuscarProductosService>().ParaGridAsync(null);

        Clientes.Clear();
        foreach (var c in clientes) Clientes.Add(c);
        ClienteSeleccionado = Clientes.FirstOrDefault();

        Variantes.Clear();
        foreach (var v in variantes) Variantes.Add(v);
    }

    [RelayCommand]
    private void AgregarLinea()
    {
        Error = null;
        if (VarianteSeleccionada is null) { Error = "Selecciona un producto."; return; }
        if (Cantidad <= 0) { Error = "La cantidad debe ser mayor que cero."; return; }

        Lineas.Add(new LineaApartadoEditable
        {
            VarianteId = VarianteSeleccionada.VarianteId,
            Descripcion = VarianteSeleccionada.Descripcion,
            Cantidad = Cantidad,
            Precio = Precio
        });
        OnPropertyChanged(nameof(Total));
        Cantidad = 1;
    }

    [RelayCommand]
    private void QuitarLinea(LineaApartadoEditable linea)
    {
        Lineas.Remove(linea);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        Error = null;
        if (ClienteSeleccionado is null) { Error = "Selecciona un cliente."; return; }
        if (Lineas.Count == 0) { Error = "Agrega al menos una línea."; return; }
        if (AnticipoInicial > Total) { Error = "El anticipo no puede exceder el total."; return; }

        var dto = new NuevoApartadoDto(
            ClienteSeleccionado.Id,
            Lineas.Select(l => new NuevaLineaApartadoDto(l.VarianteId, l.Cantidad, l.Precio)).ToList(),
            AnticipoInicial,
            MetodoAnticipo);

        Guardando = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ApartadoService>();
            var r = await servicio.CrearAsync(dto);
            if (r.EsFallo) { Error = r.Error; return; }
            Cerrar?.Invoke(true);
        }
        finally { Guardando = false; }
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
