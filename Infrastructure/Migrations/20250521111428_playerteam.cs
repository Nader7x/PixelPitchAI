using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class playerteam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_StatsBombPlayerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "StatsBombPlayerId",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatsBombPlayerId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_StatsBombPlayerId",
                table: "Players",
                column: "StatsBombPlayerId",
                unique: true,
                filter: "\"StatsBombPlayerId\" IS NOT NULL");
        }
    }
}
