using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CummulativeQuantity",
                table: "BinanceFuturesOrders",
                newName: "CumulativeQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CumulativeQuantity",
                table: "BinanceFuturesOrders",
                newName: "CummulativeQuantity");
        }
    }
}
