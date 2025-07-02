using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class NewKeyLogic2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Klines_SymbolId",
                table: "Klines",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_Klines_SymbolId_OpenTime",
                table: "Klines",
                columns: new[] { "SymbolId", "OpenTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Klines_SymbolId",
                table: "Klines");

            migrationBuilder.DropIndex(
                name: "IX_Klines_SymbolId_OpenTime",
                table: "Klines");
        }
    }
}
