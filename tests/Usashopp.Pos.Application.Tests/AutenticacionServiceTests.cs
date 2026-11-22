using FluentAssertions;
using NSubstitute;
using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Usuarios;
using Usashopp.Pos.Domain.Entities;
using Xunit;

namespace Usashopp.Pos.Application.Tests;

public class AutenticacionServiceTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IRepository<RegistroAuditoria> _auditoria = Substitute.For<IRepository<RegistroAuditoria>>();
    private readonly IDateTime _reloj = Substitute.For<IDateTime>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private AutenticacionService CrearServicio() => new(_usuarios, _hasher, _auditoria, _reloj, _uow);

    private Usuario SupervisorCon(params string[] permisos)
    {
        var rol = new Rol { Nombre = "Encargado" };
        foreach (var p in permisos) rol.Permisos.Add(new Permiso { Clave = p });
        return new Usuario { Nombre = "Súper", UsuarioLogin = "super", HashContrasena = "hash", Activo = true, Rol = rol };
    }

    [Fact]
    public async Task Autorizar_supervisor_valido_con_permiso_devuelve_nombre()
    {
        _usuarios.ObtenerPorLoginAsync("super", Arg.Any<CancellationToken>())
            .Returns(SupervisorCon(Permisos.DescuentosAplicar));
        _hasher.Verificar("1234", "hash").Returns(true);
        var servicio = CrearServicio();

        var r = await servicio.AutorizarAsync("super", "1234", Permisos.DescuentosAplicar, "aplicar descuento");

        r.Exito.Should().BeTrue();
        r.Valor.Should().Be("Súper");
    }

    [Fact]
    public async Task Autorizar_contrasena_incorrecta_falla()
    {
        _usuarios.ObtenerPorLoginAsync("super", Arg.Any<CancellationToken>())
            .Returns(SupervisorCon(Permisos.DescuentosAplicar));
        _hasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var servicio = CrearServicio();

        var r = await servicio.AutorizarAsync("super", "mala", Permisos.DescuentosAplicar, "aplicar descuento");

        r.EsFallo.Should().BeTrue();
    }

    [Fact]
    public async Task Autorizar_supervisor_sin_permiso_falla()
    {
        _usuarios.ObtenerPorLoginAsync("super", Arg.Any<CancellationToken>())
            .Returns(SupervisorCon(Permisos.VentasCrear)); // no tiene descuentos.aplicar
        _hasher.Verificar("1234", "hash").Returns(true);
        var servicio = CrearServicio();

        var r = await servicio.AutorizarAsync("super", "1234", Permisos.DescuentosAplicar, "aplicar descuento");

        r.EsFallo.Should().BeTrue();
        r.Error.Should().Contain("permiso");
    }

    [Fact]
    public async Task Autorizar_con_pin_identifica_al_supervisor_y_valida_permiso()
    {
        var super = SupervisorCon(Permisos.DescuentosAplicar);
        super.PinHash = "pinhash";
        var cajero = SupervisorCon();
        cajero.Nombre = "Caja";
        cajero.PinHash = "otro";
        _usuarios.ListarConPinAsync(Arg.Any<CancellationToken>()).Returns(new List<Usuario> { cajero, super });
        _hasher.Verificar("4321", "pinhash").Returns(true);

        var ok = await CrearServicio().AutorizarConPinAsync("4321", Permisos.DescuentosAplicar, "descuento");
        var malo = await CrearServicio().AutorizarConPinAsync("9999", Permisos.DescuentosAplicar, "descuento");
        var corto = await CrearServicio().AutorizarConPinAsync("12", Permisos.DescuentosAplicar, "descuento");

        ok.Valor.Should().Be("Súper");
        malo.EsFallo.Should().BeTrue();
        corto.EsFallo.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234", true)]
    [InlineData("123456", true)]
    [InlineData("123", false)]
    [InlineData("1234567", false)]
    [InlineData("12a4", false)]
    public void Reglas_del_pin(string pin, bool valido) => AutenticacionService.PinValido(pin).Should().Be(valido);
}
