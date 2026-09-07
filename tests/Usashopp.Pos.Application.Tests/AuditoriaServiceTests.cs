using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Auditoria;
using Usashopp.Pos.Application.Auditoria.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class AuditoriaServiceTests
{
    private readonly IRepository<RegistroAuditoria> _repo = Substitute.For<IRepository<RegistroAuditoria>>();
    private readonly ICurrentUser _usuario = Substitute.For<ICurrentUser>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private AuditoriaService CrearServicio() => new(_repo, _usuario, _reloj, _uow);

    [Fact]
    public async Task Registrar_sella_usuario_actual_y_guarda()
    {
        _usuario.UsuarioId.Returns(Guid.NewGuid());
        _usuario.Nombre.Returns("Lalo");
        _reloj.UtcAhora.Returns(new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc));
        var servicio = CrearServicio();

        await servicio.RegistrarAsync("Apertura de caja", "Fondo $500", "SesionCaja");

        await _repo.Received(1).AgregarAsync(
            Arg.Is<RegistroAuditoria>(r =>
                r.Accion == "Apertura de caja" && r.UsuarioNombre == "Lalo" &&
                r.Detalle == "Fondo $500" && r.Entidad == "SesionCaja"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Registrar_es_best_effort_no_propaga_excepciones()
    {
        _repo.AgregarAsync(Arg.Any<RegistroAuditoria>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));
        var servicio = CrearServicio();

        var acto = async () => await servicio.RegistrarAsync("Algo");

        await acto.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Listar_filtra_por_texto_y_ordena_desc()
    {
        var registros = new List<RegistroAuditoria>
        {
            new() { Fecha = new DateTime(2026, 1, 1), Accion = "Inicio de sesión", UsuarioNombre = "Ana" },
            new() { Fecha = new DateTime(2026, 3, 1), Accion = "Cancelación de venta", UsuarioNombre = "Beto", Detalle = "Venta V-1" },
            new() { Fecha = new DateTime(2026, 2, 1), Accion = "Apertura de caja", UsuarioNombre = "Ana" },
        };
        _repo.ListarAsync(Arg.Any<Expression<Func<RegistroAuditoria, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(registros);
        var servicio = CrearServicio();

        var soloAna = await servicio.ListarAsync(new FiltroAuditoriaDto(Texto: "ana"));

        soloAna.Should().HaveCount(2);
        soloAna.Should().OnlyContain(r => r.Usuario == "Ana");
        // Orden descendente por fecha: primero la apertura (feb) y luego el login (ene).
        soloAna[0].Accion.Should().Be("Apertura de caja");
    }
}
