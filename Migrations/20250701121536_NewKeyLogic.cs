using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class NewKeyLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Klines",
                table: "Klines");

            migrationBuilder.AlterColumn<int>(
                name: "KlineId",
                table: "Klines",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Klines",
                table: "Klines",
                columns: new[] { "SymbolId", "OpenTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Klines",
                table: "Klines");

            migrationBuilder.AlterColumn<int>(
                name: "KlineId",
                table: "Klines",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Klines",
                table: "Klines",
                column: "KlineId");
        }
    }
}
