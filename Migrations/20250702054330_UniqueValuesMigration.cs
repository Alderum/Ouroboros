using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class UniqueValuesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Klines_CloseTime",
                table: "Klines",
                column: "CloseTime",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Klines_OpenTime",
                table: "Klines",
                column: "OpenTime",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Klines_CloseTime",
                table: "Klines");

            migrationBuilder.DropIndex(
                name: "IX_Klines_OpenTime",
                table: "Klines");
        }
    }
}
