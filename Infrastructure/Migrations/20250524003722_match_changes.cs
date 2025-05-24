using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class match_changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Seasons_SeasonId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Match_SeasonRound",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Match_Teams_Season",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SeasonId",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "MatchStatus",
                table: "Matches",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamSeasonId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamSeasonId",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamSeasonId",
                table: "Matches",
                column: "AwayTeamSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamSeasonId",
                table: "Matches",
                column: "HomeTeamSeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Seasons_AwayTeamSeasonId",
                table: "Matches",
                column: "AwayTeamSeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Seasons_HomeTeamSeasonId",
                table: "Matches",
                column: "HomeTeamSeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Seasons_AwayTeamSeasonId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Seasons_HomeTeamSeasonId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_AwayTeamSeasonId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_HomeTeamSeasonId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamSeasonId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamSeasonId",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "MatchStatus",
                table: "Matches",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Match_SeasonRound",
                table: "Matches",
                columns: new[] { "SeasonId", "MatchWeek" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_Teams_Season",
                table: "Matches",
                columns: new[] { "HomeTeamId", "AwayTeamId", "SeasonId" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Seasons_SeasonId",
                table: "Matches",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
