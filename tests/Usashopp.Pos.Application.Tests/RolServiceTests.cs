using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Domain.Entities;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class RolServiceTests
{
    private readonly IRolRepository _roles = Substitute.For<IRolRepository>();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IRepository<Permiso> _permisos = Substitute.For<IRepository<Permiso>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditoria _auditoria = Substitute.For<IAuditoria>();

    public RolServiceTests()
    {
        // Catálogo de permisos disponible para resolver claves -> entidades.
        _permisos.ListarAsync(Arg.Any<Expression<Func<Permiso, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(Permisos.Todos.Select(c => new Permiso { Clave = c }).ToList());
        _usuarios.ListarAsync(Arg.Any<Expression<Func<Usuario, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Usuario>());
    }

    private RolService CrearServicio() => new(_roles, _usuarios, _permisos, _uow, _auditoria);

    private void RolesExistentes(params Rol[] roles) =>
        _roles.ListarConPermisosAsync(Arg.Any<CancellationToken>()).Returns(roles.ToList());

    [Fact]
    public async Task Crear_nombre_corto_falla()
    {
        RolesExistentes();
        var r = await CrearServicio().CrearAsync(new GuardarRolDto(Guid.Empty, "ab", new[] { Permisos.VentasCrear }));
        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Crear_nombre_duplicado_falla()
    {
        RolesExistentes(new Rol { Nombre = "Cajero" });
        var r = await CrearServicio().CrearAsync(new GuardarRolDto(Guid.Empty, "cajero", new[] { Permisos.VentasCrear }));
        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Crear_ok_agrega_rol_con_permisos_resueltos()
    {
        RolesExistentes();
        var servicio = CrearServicio();

        var r = await servicio.CrearAsync(new GuardarRolDto(
            Guid.Empty, "Supervisor", new[] { Permisos.VentasCrear, Permisos.CajaCorte }));

        r.Exito.Should().BeTrue();
        await _roles.Received(1).AgregarAsync(
            Arg.Is<Rol>(x => x.Nombre == "Supervisor" && x.Permisos.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Actualizar_rol_administrador_esta_protegido()
    {
        var admin = new Rol { Nombre = "Administrador" };
        RolesExistentes(admin);
        var r = await CrearServicio().ActualizarAsync(new GuardarRolDto(admin.Id, "Administrador", Array.Empty<string>()));
        r.EsFallo.Should().BeTrue();
        r.Error.Should().Contain("Administrador");
    }

    [Fact]
    public async Task Eliminar_administrador_esta_protegido()
    {
        var admin = new Rol { Nombre = "Administrador" };
        RolesExistentes(admin);
        var r = await CrearServicio().EliminarAsync(admin.Id);
        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Eliminar_rol_con_usuarios_falla()
    {
        var rol = new Rol { Nombre = "Cajero" };
        RolesExistentes(rol);
        _usuarios.ListarAsync(Arg.Any<Expression<Func<Usuario, bool>>?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Usuario> { new() { RolId = rol.Id } });

        var r = await CrearServicio().EliminarAsync(rol.Id);

        r.EsFallo.Should().BeTrue();
        _roles.DidNotReceive().Eliminar(Arg.Any<Rol>());
    }
}
