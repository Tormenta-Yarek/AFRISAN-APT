using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Afrisan.Api.Migrations
{
    /// <inheritdoc />
    public partial class CodigoQrUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Cilindros_CodigoQr",
                table: "Cilindros",
                column: "CodigoQr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cilindros_CodigoQr",
                table: "Cilindros");
        }
    }
}
