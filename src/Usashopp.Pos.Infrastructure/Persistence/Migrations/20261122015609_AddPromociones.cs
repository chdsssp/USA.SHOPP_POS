using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromociones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPromocion",
                table: "DetallesVenta",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PromocionId",
                table: "DetallesVenta",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromocionNombre",
                table: "DetallesVenta",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Promociones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Alcance = table.Column<int>(type: "INTEGER", nullable: false),
                    AlcanceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    N = table.Column<int>(type: "INTEGER", nullable: false),
                    M = table.Column<int>(type: "INTEGER", nullable: false),
                    Inicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Fin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DiasSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Prioridad = table.Column<int>(type: "INTEGER", nullable: false),
                    AcumulableConManual = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promociones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promociones_Activa",
                table: "Promociones",
                column: "Activa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Promociones");

            migrationBuilder.DropColumn(
                name: "DescuentoPromocion",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "PromocionId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "PromocionNombre",
                table: "DetallesVenta");
        }
    }
}
