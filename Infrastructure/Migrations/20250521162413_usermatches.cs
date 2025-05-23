using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class usermatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CreatorId",
                table: "Matches",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_CreatorId",
                table: "Matches",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_CreatorId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_CreatorId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Matches");
        }
    }
}
