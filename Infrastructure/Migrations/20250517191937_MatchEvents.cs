using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MatchEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HalfTimeAwayScore",
                table: "MatchEvents");

            migrationBuilder.RenameColumn(
                name: "HalfTimeHomeScore",
                table: "MatchEvents",
                newName: "TotalCorners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalCorners",
                table: "MatchEvents",
                newName: "HalfTimeHomeScore");

            migrationBuilder.AddColumn<int>(
                name: "HalfTimeAwayScore",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
