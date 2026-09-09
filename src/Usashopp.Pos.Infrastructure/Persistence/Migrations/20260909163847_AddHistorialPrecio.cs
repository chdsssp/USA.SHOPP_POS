using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usashopp.Pos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialPrecio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialPrecios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VarianteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecioAnterior = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioNuevo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPrecios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPrecios_Variantes_VarianteId",
                        column: x => x.VarianteId,
                        principalTable: "Variantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_Fecha",
                table: "HistorialPrecios",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecios_VarianteId",
                table: "HistorialPrecios",
                column: "VarianteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialPrecios");
        }
    }
}
