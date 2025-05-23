using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class matchandevents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_CreatorId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "TotalBlocks",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalClearances",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDribbles",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDuels",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalErrors",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalFreeKicks",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGoalKicks",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGoalkeeperSaves",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGoals",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInjuries",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInterceptions",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOffsides",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOuts",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPenalties",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPossessionWon",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRedCards",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSubstitutions",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalThrowIns",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalYellowCards",
                table: "MatchEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "CreatorId",
                table: "Matches",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AwayAccurateLongBalls",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayLongBalls",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamClearances",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamDribbles",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamDuels",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamDuelsWon",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamFreeKicks",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamGoalKicks",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwayTeamInMatchName",
                table: "Matches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamLongBallsAccuracy",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamOffsides",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamPassAccuracy",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamPasses",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamPassesCompleted",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AwayTeamPossessionDurationSeconds",
                table: "Matches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamPossessionWon",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamRecoveries",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamSaves",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayTeamShotsOffTarget",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeAccurateLongBalls",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeLongBalls",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamClearances",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamDribbles",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamDuels",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamDuelsWon",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamFreeKicks",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamGoalKicks",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeTeamInMatchName",
                table: "Matches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamLongBallsAccuracy",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamOffsides",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamPassAccuracy",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamPasses",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamPassesCompleted",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HomeTeamPossessionDurationSeconds",
                table: "Matches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamPossessionWon",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamRecoveries",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamSaves",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeTeamShotsOffTarget",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEventPossessingTeamName",
                table: "Matches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastEventTimestampSeconds",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_LastUpdated",
                table: "MatchEvents",
                column: "LastUpdated")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_CreatorId",
                table: "Matches",
                column: "CreatorId")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_StadiumId",
                table: "Matches",
                column: "StadiumId")
                .Annotation("Npgsql:IndexMethod", "btree");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchEvents_LastUpdated",
                table: "MatchEvents");

            migrationBuilder.DropIndex(
                name: "IX_Match_CreatorId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Match_StadiumId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TotalBlocks",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalClearances",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalDribbles",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalDuels",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalErrors",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalFreeKicks",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalGoalKicks",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalGoalkeeperSaves",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalGoals",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalInjuries",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalInterceptions",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalOffsides",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalOuts",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalPenalties",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalPossessionWon",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalRedCards",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalSubstitutions",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalThrowIns",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "TotalYellowCards",
                table: "MatchEvents");

            migrationBuilder.DropColumn(
                name: "AwayAccurateLongBalls",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayLongBalls",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamClearances",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamDribbles",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamDuels",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamDuelsWon",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamFreeKicks",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamGoalKicks",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamInMatchName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamLongBallsAccuracy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamOffsides",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamPassAccuracy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamPasses",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamPassesCompleted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamPossessionDurationSeconds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamPossessionWon",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamRecoveries",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamSaves",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamShotsOffTarget",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeAccurateLongBalls",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeLongBalls",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamClearances",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamDribbles",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamDuels",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamDuelsWon",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamFreeKicks",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamGoalKicks",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamInMatchName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamLongBallsAccuracy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamOffsides",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamPassAccuracy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamPasses",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamPassesCompleted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamPossessionDurationSeconds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamPossessionWon",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamRecoveries",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamSaves",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamShotsOffTarget",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LastEventPossessingTeamName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LastEventTimestampSeconds",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "CreatorId",
                table: "Matches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CreatorId",
                table: "Matches",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches",
                column: "StadiumId");
        }
    }
}
