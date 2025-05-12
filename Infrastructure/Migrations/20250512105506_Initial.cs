using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LeagueName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TotalRounds = table.Column<int>(type: "integer", nullable: true),
                    CurrentRound = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                },
                comment: "Football competition seasons");

            migrationBuilder.CreateTable(
                name: "Stadiums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    BuiltDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRenovation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SurfaceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    HasRoof = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", maxLength: 1000, nullable: false),
                    Facilities = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stadiums", x => x.Id);
                },
                comment: "Stadium information");

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Logo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    League = table.Column<string>(type: "text", nullable: false),
                    StadiumId = table.Column<int>(type: "integer", nullable: true),
                    FoundationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrimaryColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Football teams information");

            migrationBuilder.CreateTable(
                name: "Coaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nationality = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    PreferredFormation = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CoachingStyle = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coaches_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Football coaches information");

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    KnownName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Nationality = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ShirtNumber = table.Column<short>(type: "smallint", nullable: true),
                    PreferredFoot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    PhotoUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    StatsBombPlayerId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Football players information");

            migrationBuilder.CreateTable(
                name: "TeamSeasonStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    GoalsScored = table.Column<int>(type: "integer", nullable: false),
                    GoalsConceded = table.Column<int>(type: "integer", nullable: false),
                    GoalDifference = table.Column<int>(type: "integer", nullable: false),
                    CleanSheets = table.Column<int>(type: "integer", nullable: false),
                    YellowCards = table.Column<int>(type: "integer", nullable: false),
                    RedCards = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Form = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AveragePossession = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PassAccuracy = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Shots = table.Column<int>(type: "integer", nullable: false),
                    ShotsOnTarget = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Corners = table.Column<int>(type: "integer", nullable: false),
                    Fouls = table.Column<int>(type: "integer", nullable: false),
                    ExpectedGoals = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ExpectedGoalsAgainst = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    HomeWins = table.Column<int>(type: "integer", nullable: false),
                    HomeDraws = table.Column<int>(type: "integer", nullable: false),
                    HomeLosses = table.Column<int>(type: "integer", nullable: false),
                    HomeGoalsScored = table.Column<int>(type: "integer", nullable: false),
                    HomeGoalsConceded = table.Column<int>(type: "integer", nullable: false),
                    AwayWins = table.Column<int>(type: "integer", nullable: false),
                    AwayDraws = table.Column<int>(type: "integer", nullable: false),
                    AwayLosses = table.Column<int>(type: "integer", nullable: false),
                    AwayGoalsScored = table.Column<int>(type: "integer", nullable: false),
                    AwayGoalsConceded = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    TeamId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSeasonStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStats_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamSeasonStats_Teams_TeamId1",
                        column: x => x.TeamId1,
                        principalTable: "Teams",
                        principalColumn: "Id");
                },
                comment: "Team statistics by season");

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: false),
                    AwayTeamId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDateTimeUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StadiumId = table.Column<int>(type: "integer", nullable: true),
                    MatchWeek = table.Column<short>(type: "smallint", nullable: true),
                    HomeCoachId = table.Column<int>(type: "integer", nullable: true),
                    AwayCoachId = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamScore = table.Column<short>(type: "smallint", nullable: true),
                    AwayTeamScore = table.Column<short>(type: "smallint", nullable: true),
                    WinningTeamId = table.Column<int>(type: "integer", nullable: true),
                    LosingTeamId = table.Column<int>(type: "integer", nullable: true),
                    IsDraw = table.Column<bool>(type: "boolean", nullable: false),
                    MatchStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    ModelSimulationStartTimeUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HomeTeamPossession = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamPossession = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamShots = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamShots = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamShotsOnTarget = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamShotsOnTarget = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamCorners = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamCorners = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamFouls = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamFouls = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamYellowCards = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamYellowCards = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamRedCards = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamRedCards = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    TeamId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Coaches_AwayCoachId",
                        column: x => x.AwayCoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Coaches_HomeCoachId",
                        column: x => x.HomeCoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matches_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamId1",
                        column: x => x.TeamId1,
                        principalTable: "Teams",
                        principalColumn: "Id");
                },
                comment: "Football match information");

            migrationBuilder.CreateTable(
                name: "PlayerSeasonStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    Appearances = table.Column<int>(type: "integer", nullable: false),
                    MinutesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Goals = table.Column<int>(type: "integer", nullable: false),
                    Assists = table.Column<int>(type: "integer", nullable: false),
                    YellowCards = table.Column<int>(type: "integer", nullable: false),
                    RedCards = table.Column<int>(type: "integer", nullable: false),
                    CleanSheets = table.Column<int>(type: "integer", nullable: false),
                    Saves = table.Column<int>(type: "integer", nullable: false),
                    Tackles = table.Column<int>(type: "integer", nullable: false),
                    Interceptions = table.Column<int>(type: "integer", nullable: false),
                    Clearances = table.Column<int>(type: "integer", nullable: false),
                    BlockedShots = table.Column<int>(type: "integer", nullable: false),
                    PassesCompleted = table.Column<int>(type: "integer", nullable: false),
                    PassesAttempted = table.Column<int>(type: "integer", nullable: false),
                    PassCompletionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    KeyPasses = table.Column<int>(type: "integer", nullable: false),
                    ChancesCreated = table.Column<int>(type: "integer", nullable: false),
                    ShotsOnTarget = table.Column<int>(type: "integer", nullable: false),
                    ShotsOffTarget = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", precision: 3, scale: 1, nullable: true),
                    PlayerId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasonStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Players_PlayerId1",
                        column: x => x.PlayerId1,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Player statistics by season");

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventsJson = table.Column<string>(type: "jsonb", nullable: false),
                    GoalsHomeTeam = table.Column<int>(type: "integer", nullable: false),
                    GoalsAwayTeam = table.Column<int>(type: "integer", nullable: false),
                    TotalEvents = table.Column<int>(type: "integer", nullable: false),
                    TotalShots = table.Column<int>(type: "integer", nullable: false),
                    TotalPasses = table.Column<int>(type: "integer", nullable: false),
                    TotalFouls = table.Column<int>(type: "integer", nullable: false),
                    TotalCards = table.Column<int>(type: "integer", nullable: false),
                    HalfTimeHomeScore = table.Column<int>(type: "integer", nullable: false),
                    HalfTimeAwayScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Contains event data for matches");

            migrationBuilder.CreateIndex(
                name: "IX_Coach_Name",
                table: "Coaches",
                columns: new[] { "FirstName", "LastName" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Coach_Nationality",
                table: "Coaches",
                column: "Nationality")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Coach_TeamId",
                table: "Coaches",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_AwayTeam",
                table: "Matches",
                column: "AwayTeamId")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_HomeTeam",
                table: "Matches",
                column: "HomeTeamId")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_KickoffTime",
                table: "Matches",
                column: "ScheduledDateTimeUTC")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_SeasonRound",
                table: "Matches",
                columns: new[] { "SeasonId", "MatchWeek" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_Status",
                table: "Matches",
                column: "MatchStatus")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Match_Teams_Season",
                table: "Matches",
                columns: new[] { "HomeTeamId", "AwayTeamId", "SeasonId" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayCoachId",
                table: "Matches",
                column: "AwayCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeCoachId",
                table: "Matches",
                column: "HomeCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId",
                table: "Matches",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId1",
                table: "Matches",
                column: "TeamId1");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_EventsJson",
                table: "MatchEvents",
                column: "EventsJson")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_Goals",
                table: "MatchEvents",
                columns: new[] { "GoalsHomeTeam", "GoalsAwayTeam" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchId",
                table: "MatchEvents",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_Name",
                table: "Players",
                columns: new[] { "FullName", "KnownName" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Player_Nationality",
                table: "Players",
                column: "Nationality")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Player_TeamId",
                table: "Players",
                column: "TeamId")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Players_FullName",
                table: "Players",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Players_StatsBombPlayerId",
                table: "Players",
                column: "StatsBombPlayerId",
                unique: true,
                filter: "\"StatsBombPlayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_Assists",
                table: "PlayerSeasonStats",
                columns: new[] { "SeasonId", "Assists" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_Goals",
                table: "PlayerSeasonStats",
                columns: new[] { "SeasonId", "Goals" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_PlayerId1",
                table: "PlayerSeasonStats",
                column: "PlayerId1");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_PlayerSeason",
                table: "PlayerSeasonStats",
                columns: new[] { "PlayerId", "SeasonId", "TeamId" },
                unique: true,
                filter: "\"TeamId\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_Rating",
                table: "PlayerSeasonStats",
                columns: new[] { "SeasonId", "Rating" },
                filter: "\"Rating\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonStats_TeamId",
                table: "PlayerSeasonStats",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_Active",
                table: "Seasons",
                column: "IsActive")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Season_CurrentRound",
                table: "Seasons",
                columns: new[] { "IsActive", "CurrentRound" },
                filter: "\"IsActive\" = true")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Season_LeagueSeason",
                table: "Seasons",
                columns: new[] { "LeagueName", "Country", "Name" },
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Stadium_Facilities",
                table: "Stadiums",
                column: "Facilities")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Stadium_Location",
                table: "Stadiums",
                columns: new[] { "City", "Country" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Stadium_Name",
                table: "Stadiums",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Team_Location",
                table: "Teams",
                columns: new[] { "Country", "City" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Team_Name",
                table: "Teams",
                column: "Name",
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Team_ShortName",
                table: "Teams",
                column: "ShortName",
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_StadiumId",
                table: "Teams",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_AwayPerformance",
                table: "TeamSeasonStats",
                columns: new[] { "SeasonId", "AwayWins", "AwayDraws", "AwayLosses" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_HomePerformance",
                table: "TeamSeasonStats",
                columns: new[] { "SeasonId", "HomeWins", "HomeDraws", "HomeLosses" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_Position",
                table: "TeamSeasonStats",
                columns: new[] { "SeasonId", "Position" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_Standings",
                table: "TeamSeasonStats",
                columns: new[] { "SeasonId", "Points", "GoalDifference", "GoalsScored" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_TeamId1",
                table: "TeamSeasonStats",
                column: "TeamId1");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSeasonStats_TeamSeason",
                table: "TeamSeasonStats",
                columns: new[] { "TeamId", "SeasonId" },
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "PlayerSeasonStats");

            migrationBuilder.DropTable(
                name: "TeamSeasonStats");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Coaches");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Stadiums");
        }
    }
}
