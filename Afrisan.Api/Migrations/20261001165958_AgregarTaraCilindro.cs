using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Afrisan.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTaraCilindro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaraKg",
                table: "Cilindros",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaraKg",
                table: "Cilindros");
        }
    }
}
