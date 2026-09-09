using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteCreditoFiscalLealtad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DireccionFiscal",
                table: "Clientes",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LimiteCredito",
                table: "Clientes",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Puntos",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "Clientes",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegimenFiscal",
                table: "Clientes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rfc",
                table: "Clientes",
                type: "TEXT",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsoCfdi",
                table: "Clientes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbonosCliente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Metodo = table.Column<int>(type: "INTEGER", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonosCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbonosCliente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCliente_ClienteId",
                table: "AbonosCliente",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbonosCliente");

            migrationBuilder.DropColumn(
                name: "DireccionFiscal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "LimiteCredito",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Puntos",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "RegimenFiscal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Rfc",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "UsoCfdi",
                table: "Clientes");
        }
    }
}
