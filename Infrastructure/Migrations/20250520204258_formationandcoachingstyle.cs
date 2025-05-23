using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class formationandcoachingstyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoachingStyle",
                table: "Coaches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredFormation",
                table: "Coaches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoachingStyle",
                table: "Coaches");

            migrationBuilder.DropColumn(
                name: "PreferredFormation",
                table: "Coaches");
        }
    }
}
