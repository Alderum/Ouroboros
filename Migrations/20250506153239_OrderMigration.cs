using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class OrderMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BinanceFuturesOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Pair = table.Column<string>(type: "TEXT", nullable: true),
                    ClientOrderId = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantityFilled = table.Column<decimal>(type: "TEXT", nullable: false),
                    CummulativeQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    QuoteQuantityFilled = table.Column<decimal>(type: "TEXT", nullable: true),
                    BaseQuantityFilled = table.Column<decimal>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReduceOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClosePosition = table.Column<bool>(type: "INTEGER", nullable: false),
                    Side = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StopPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    TimeInForce = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalType = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivatePrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CallbackRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkingType = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionSide = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceProtect = table.Column<bool>(type: "INTEGER", nullable: false),
                    PriceMatch = table.Column<int>(type: "INTEGER", nullable: false),
                    SelfTradePreventionMode = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinanceFuturesOrders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinanceFuturesOrders");
        }
    }
}
