using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class IBinanceKline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ClosePrice",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CloseTime",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "HighPrice",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LowPrice",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenPrice",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenTime",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "QuoteVolume",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TakerBuyBaseVolume",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TakerBuyQuoteVolume",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TradeCount",
                table: "Klines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosePrice",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "CloseTime",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "HighPrice",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "LowPrice",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "OpenPrice",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "OpenTime",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "QuoteVolume",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "TakerBuyBaseVolume",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "TakerBuyQuoteVolume",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "TradeCount",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Klines");
        }
    }
}
