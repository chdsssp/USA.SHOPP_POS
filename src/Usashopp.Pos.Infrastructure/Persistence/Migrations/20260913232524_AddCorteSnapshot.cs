using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorteSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CorteEfectivoEsperado",
                table: "SesionesCaja",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorteNumVentas",
                table: "SesionesCaja",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CorteTotalEfectivo",
                table: "SesionesCaja",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CorteTotalVentas",
                table: "SesionesCaja",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorteEfectivoEsperado",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "CorteNumVentas",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "CorteTotalEfectivo",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "CorteTotalVentas",
                table: "SesionesCaja");
        }
    }
}
