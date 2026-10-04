using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Afrisan.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGasRefrigerante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GasRefrigeranteId",
                table: "Cilindros",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GasesRefrigerantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GasesRefrigerantes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cilindros_GasRefrigeranteId",
                table: "Cilindros",
                column: "GasRefrigeranteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cilindros_GasesRefrigerantes_GasRefrigeranteId",
                table: "Cilindros",
                column: "GasRefrigeranteId",
                principalTable: "GasesRefrigerantes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cilindros_GasesRefrigerantes_GasRefrigeranteId",
                table: "Cilindros");

            migrationBuilder.DropTable(
                name: "GasesRefrigerantes");

            migrationBuilder.DropIndex(
                name: "IX_Cilindros_GasRefrigeranteId",
                table: "Cilindros");

            migrationBuilder.DropColumn(
                name: "GasRefrigeranteId",
                table: "Cilindros");
        }
    }
}
