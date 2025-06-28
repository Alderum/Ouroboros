using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBTBotConsole3.Migrations
{
    /// <inheritdoc />
    public partial class BinanceKline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Close",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "High",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "Low",
                table: "Klines");

            migrationBuilder.DropColumn(
                name: "Open",
                table: "Klines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Close",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "High",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Klines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Low",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Open",
                table: "Klines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
