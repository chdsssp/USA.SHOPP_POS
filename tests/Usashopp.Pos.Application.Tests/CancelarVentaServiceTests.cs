using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Ventas;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class CancelarVentaServiceTests
{
    private readonly IVentaRepository _ventas = Substitute.For<IVentaRepository>();
    private readonly IVarianteRepository _variantes = Substitute.For<IVarianteRepository>();
    private readonly IMovimientoInventarioRepository _movInv = Substitute.For<IMovimientoInventarioRepository>();
    private readonly IRepository<NotaCredito> _notasCredito = Substitute.For<IRepository<NotaCredito>>();
    private readonly IRepository<ConsumoNotaCredito> _consumosNota = Substitute.For<IRepository<ConsumoNotaCredito>>();
    private readonly IRepository<Cliente> _clientes = Substitute.For<IRepository<Cliente>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    private readonly Guid _varId = Guid.NewGuid();

    public CancelarVentaServiceTests()
    {
        _usuario.UsuarioId.Returns(Guid.NewGuid());
        _reloj.UtcAhora.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _uow.EjecutarEnTransaccionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _variantes.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new VarianteProducto());
        _consumosNota.ListarAsync(Arg.Any<Expression<Func<ConsumoNotaCredito, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ConsumoNotaCredito>());
    }

    private CancelarVentaService CrearServicio() => new(
        _ventas, _variantes, _movInv, _notasCredito, _consumosNota, _clientes,
        _usuario, _reloj, _uow, _auditoria);

    private Venta CrearVenta(Guid? clienteId = null)
    {
        var venta = new Venta { Folio = "V-001", ClienteId = clienteId };
        venta.Detalles.Add(new DetalleVenta
        {
            VarianteId = _varId, Descripcion = "A", Cantidad = 1, PrecioUnitario = new Dinero(100m)
        });
        venta.MarcarPagada();
        _ventas.ObtenerConDetalleAsync(venta.Id, Arg.Any<CancellationToken>()).Returns(venta);
        return venta;
    }

    [Fact]
    public async Task Cancelar_reintegra_stock_y_marca_cancelada()
    {
        var venta = CrearVenta();

        var r = await CrearServicio().EjecutarAsync(venta.Id);

        r.Exito.Should().BeTrue();
        venta.Estado.Should().Be(EstadoVenta.Cancelada);
        await _movInv.Received(1).AgregarAsync(
            Arg.Is<MovimientoInventario>(m => m.Tipo == TipoMovimientoInventario.Devolucion && m.Cantidad == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelar_revierte_puntos_de_lealtad()
    {
        var clienteId = Guid.NewGuid();
        var venta = CrearVenta(clienteId); // Total 100 → 10 puntos otorgados
        var cliente = new Cliente { Nombre = "Cli", Puntos = 25 };
        _clientes.ObtenerPorIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(cliente);

        var r = await CrearServicio().EjecutarAsync(venta.Id);

        r.Exito.Should().BeTrue();
        cliente.Puntos.Should().Be(15); // 25 - 10
    }

    [Fact]
    public async Task Cancelar_restaura_saldo_de_nota_de_credito_consumida()
    {
        var venta = CrearVenta();
        var nota = new NotaCredito { Saldo = new Dinero(0m), Estado = EstadoNotaCredito.Usada };
        _consumosNota.ListarAsync(Arg.Any<Expression<Func<ConsumoNotaCredito, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ConsumoNotaCredito>
            {
                new() { NotaCreditoId = nota.Id, VentaId = venta.Id, Monto = new Dinero(50m) }
            });
        _notasCredito.ObtenerPorIdAsync(nota.Id, Arg.Any<CancellationToken>()).Returns(nota);

        var r = await CrearServicio().EjecutarAsync(venta.Id);

        r.Exito.Should().BeTrue();
        nota.Saldo.Monto.Should().Be(50m);
        nota.Estado.Should().Be(EstadoNotaCredito.Activa);
        _consumosNota.Received(1).Eliminar(Arg.Is<ConsumoNotaCredito>(c => c.NotaCreditoId == nota.Id));
    }
}
