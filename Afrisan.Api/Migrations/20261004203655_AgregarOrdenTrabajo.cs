using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Afrisan.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrdenTrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrdenTrabajoId",
                table: "MovimientosInventario",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_OrdenTrabajoId",
                table: "MovimientosInventario",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_Codigo",
                table: "OrdenesTrabajo",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_OrdenesTrabajo_OrdenTrabajoId",
                table: "MovimientosInventario",
                column: "OrdenTrabajoId",
                principalTable: "OrdenesTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_OrdenesTrabajo_OrdenTrabajoId",
                table: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "OrdenesTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_OrdenTrabajoId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "OrdenTrabajoId",
                table: "MovimientosInventario");
        }
    }
}
