using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Usashopp.Pos.Application.Clientes;
using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Clientes;

/// <summary>
/// Buscador de clientes táctil (reemplaza el ComboBox con TODOS los clientes, que con cientos de
/// registros no se puede usar con el dedo): busca por nombre, teléfono, RFC o correo.
/// </summary>
public partial class SelectorClienteViewModel : ViewModelBase
{
    private const int Limite = 60;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogos;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _texto = string.Empty;
    [ObservableProperty] private string? _aviso;

    public ObservableCollection<ClienteDto> Resultados { get; } = new();

    /// <summary>Cliente elegido; null (con resultado aceptado) = "sin cliente".</summary>
    public ClienteDto? Elegido { get; private set; }

    public event Action<bool>? Cerrar;

    public SelectorClienteViewModel(IServiceScopeFactory scopeFactory, IDialogService dialogos)
    {
        _scopeFactory = scopeFactory;
        _dialogos = dialogos;
        _ = BuscarAsync(string.Empty);
    }

    partial void OnTextoChanged(string value) => _ = BuscarAsync(value);

    private async Task BuscarAsync(string texto)
    {
        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();
        try
        {
            await Task.Delay(150, cts.Token);
            using var scope = _scopeFactory.CreateScope();
            var lista = await scope.ServiceProvider.GetRequiredService<ClienteService>().ListarAsync(texto, cts.Token);
            if (cts.IsCancellationRequested) return;
            Resultados.Clear();
            foreach (var c in lista.Take(Limite)) Resultados.Add(c);
            Aviso = lista.Count > Limite ? $"Mostrando {Limite} de {lista.Count}. Escribe más para afinar." :
                    lista.Count == 0 ? "Sin coincidencias. Puedes crear el cliente." : null;
        }
        catch (OperationCanceledException) { /* reemplazada por una búsqueda más reciente */ }
    }

    [RelayCommand]
    private void Elegir(ClienteDto cliente)
    {
        Elegido = cliente;
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private void SinCliente()
    {
        Elegido = null;
        Cerrar?.Invoke(true);
    }

    [RelayCommand]
    private async Task NuevoAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<ClienteService>();
        var previos = (await servicio.ListarAsync()).Select(c => c.Id).ToHashSet();
        if (!_dialogos.MostrarEditorCliente(null)) return;
        var nuevo = (await servicio.ListarAsync()).FirstOrDefault(c => !previos.Contains(c.Id));
        if (nuevo is not null) Elegir(nuevo);
    }

    [RelayCommand]
    private void Cancelar() => Cerrar?.Invoke(false);
}
