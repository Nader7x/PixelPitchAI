using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddV2SimulationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AwayTeamFieldTilt",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamPPDA",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamPSXG",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamXG",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayTeamXT",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamFieldTilt",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamPPDA",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamPSXG",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamXG",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeTeamXT",
                table: "MatchStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayXG",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AwayXT",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeXG",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeXT",
                table: "Matches",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "Matches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpatialTrackCodec",
                table: "Matches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpatialTrackObjectKey",
                table: "Matches",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TacticalConfigJson",
                table: "Matches",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TacticalExperiments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExecutionTier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NSimulations = table.Column<int>(type: "integer", nullable: false),
                    BaseSeed = table.Column<int>(type: "integer", nullable: false),
                    RequestJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    CommentaryTelemetryJson = table.Column<string>(type: "jsonb", nullable: true),
                    RepresentativeSeedsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacticalExperiments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TacticalExperiments");

            migrationBuilder.DropColumn(
                name: "AwayTeamFieldTilt",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "AwayTeamPPDA",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "AwayTeamPSXG",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "AwayTeamXG",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "AwayTeamXT",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "HomeTeamFieldTilt",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "HomeTeamPPDA",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "HomeTeamPSXG",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "HomeTeamXG",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "HomeTeamXT",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "AwayXG",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayXT",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeXG",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeXT",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SpatialTrackCodec",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SpatialTrackObjectKey",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TacticalConfigJson",
                table: "Matches");
        }
    }
}
