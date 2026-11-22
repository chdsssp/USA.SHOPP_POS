using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEsperaCostoPinPuntos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "DetallesVenta",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill: las ventas existentes toman el costo VIGENTE de la variante (mejor aproximación
            // disponible; documentado). Las nuevas guardan el costo del momento de la venta.
            migrationBuilder.Sql("UPDATE DetallesVenta SET CostoUnitario = COALESCE((SELECT v.Costo FROM Variantes v WHERE v.Id = DetallesVenta.VarianteId), CostoUnitario);");

            migrationBuilder.AddColumn<bool>(
                name: "LealtadActiva",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesosPorPunto",
                table: "Configuracion",
                type: "TEXT",
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.CreateTable(
                name: "VentasSuspendidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SesionCajaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Etiqueta = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DescuentoGlobal = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EsBorrador = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentasSuspendidas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineasVentaSuspendida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VentaSuspendidaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VarianteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Descuento = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasVentaSuspendida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasVentaSuspendida_VentasSuspendidas_VentaSuspendidaId",
                        column: x => x.VentaSuspendidaId,
                        principalTable: "VentasSuspendidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineasVentaSuspendida_VentaSuspendidaId",
                table: "LineasVentaSuspendida",
                column: "VentaSuspendidaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentasSuspendidas_UsuarioId_EsBorrador",
                table: "VentasSuspendidas",
                columns: new[] { "UsuarioId", "EsBorrador" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasVentaSuspendida");

            migrationBuilder.DropTable(
                name: "VentasSuspendidas");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "LealtadActiva",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "PesosPorPunto",
                table: "Configuracion");
        }
    }
}
