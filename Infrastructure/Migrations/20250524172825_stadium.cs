using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class stadium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Architect",
                table: "Stadiums",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CostMillionsEuros",
                table: "Stadiums",
                type: "double precision",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "Stadiums",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Competitions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Competitions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Architect",
                table: "Stadiums");

            migrationBuilder.DropColumn(
                name: "CostMillionsEuros",
                table: "Stadiums");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "Stadiums");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Competitions");
        }
    }
}
