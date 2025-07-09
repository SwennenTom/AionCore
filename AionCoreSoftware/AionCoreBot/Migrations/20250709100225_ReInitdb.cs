using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AionCoreBot.Migrations
{
    /// <inheritdoc />
    public partial class ReInitdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountBalances_Accounts_AccountId",
                table: "AccountBalances");

            migrationBuilder.DropColumn(
                name: "Available",
                table: "AccountBalances");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "AccountBalances",
                newName: "Amount");

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "AccountBalances",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "BalanceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Asset = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceHistories", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_AccountBalances_Accounts_AccountId",
                table: "AccountBalances",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountBalances_Accounts_AccountId",
                table: "AccountBalances");

            migrationBuilder.DropTable(
                name: "BalanceHistories");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "AccountBalances",
                newName: "Total");

            migrationBuilder.AlterColumn<int>(
                name: "AccountId",
                table: "AccountBalances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Available",
                table: "AccountBalances",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountBalances_Accounts_AccountId",
                table: "AccountBalances",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
