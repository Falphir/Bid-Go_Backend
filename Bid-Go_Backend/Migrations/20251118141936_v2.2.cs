using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bid_Go_Backend.Migrations
{
    /// <inheritdoc />
    public partial class v22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticSelectionExecuted",
                table: "TransportRequests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomaticSelectionExecuted",
                table: "TransportRequests");
        }
    }
}
