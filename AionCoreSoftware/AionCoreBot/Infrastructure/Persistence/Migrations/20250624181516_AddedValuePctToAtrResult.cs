using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AionCoreBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedValuePctToAtrResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValuePct",
                table: "ATRResults",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValuePct",
                table: "ATRResults");
        }
    }
}
