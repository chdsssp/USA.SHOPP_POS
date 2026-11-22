using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VentaOrigenId",
                table: "Ventas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasMaximosCambio",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_VentaOrigenId",
                table: "Ventas",
                column: "VentaOrigenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_VentaOrigenId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "VentaOrigenId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "DiasMaximosCambio",
                table: "Configuracion");
        }
    }
}
