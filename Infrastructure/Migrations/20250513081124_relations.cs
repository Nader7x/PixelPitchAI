using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class relations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamId1",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonStats_Players_PlayerId1",
                table: "PlayerSeasonStats");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamSeasonStats_Teams_TeamId1",
                table: "TeamSeasonStats");

            migrationBuilder.DropIndex(
                name: "IX_TeamSeasonStats_TeamId1",
                table: "TeamSeasonStats");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSeasonStats_PlayerId1",
                table: "PlayerSeasonStats");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamId1",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                table: "TeamSeasonStats");

            migrationBuilder.DropColumn(
                name: "PlayerId1",
                table: "PlayerSeasonStats");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamId1",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamId1",
                table: "TeamSeasonStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayerId1",
                table: "PlayerSeasonStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamId1",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_TeamId1",
                table: "TeamSeasonStats",
                column: "TeamId1");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_PlayerId1",
                table: "PlayerSeasonStats",
                column: "PlayerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId",
                table: "Matches",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId1",
                table: "Matches",
                column: "TeamId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamId1",
                table: "Matches",
                column: "TeamId1",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonStats_Players_PlayerId1",
                table: "PlayerSeasonStats",
                column: "PlayerId1",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamSeasonStats_Teams_TeamId1",
                table: "TeamSeasonStats",
                column: "TeamId1",
                principalTable: "Teams",
                principalColumn: "Id");
        }
    }
}
