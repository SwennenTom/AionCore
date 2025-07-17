using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AionCoreBot.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedTradeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExchangeOrderId",
                table: "Trades",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPrice",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TakeProfitPrice",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrailingStopPercent",
                table: "Trades",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "TakeProfitPrice",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "TrailingStopPercent",
                table: "Trades");

            migrationBuilder.AlterColumn<long>(
                name: "ExchangeOrderId",
                table: "Trades",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
