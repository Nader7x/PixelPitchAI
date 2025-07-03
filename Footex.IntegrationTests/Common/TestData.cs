using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Footex.IntegrationTests.Common;

public static class TestData
{
    // Sample IDs for testing - in a real implementation, these would be
    // populated with actual test data from your test database
    private static readonly Guid _sampleMatchId = Guid.Parse(
        "f47ac10b-58cc-4372-a567-0e02b2c3d479"
    );
    private static readonly Guid _liveMatchId = Guid.Parse("b3c56789-ab12-4d3e-9f80-7d61234e5678");
    private static readonly Guid _sampleTeamId = Guid.Parse("dea12856-c198-4129-b3f3-38250d9f2152");
    private static readonly Guid _samplePlayerId = Guid.Parse(
        "7c9e6679-7425-40de-944b-e07fc1f90ae7"
    );
    private static readonly Guid _sampleSeasonId = Guid.Parse(
        "e0a8d3d0-770a-4f89-a9c0-7e6d4e1b4c0e"
    );
    private static readonly Guid _sampleStadiumId = Guid.Parse(
        "b23a3d3c-a434-4ba5-9f7a-059d041f55b3"
    );
    private static readonly Guid _sampleCoachId = Guid.Parse(
        "afc65e75-d452-4a17-94f6-c3abcd92a4b1"
    );

    public static Guid GetSampleMatchId()
    {
        return _sampleMatchId;
    }

    public static Guid GetLiveMatchId()
    {
        return _liveMatchId;
    }

    public static Guid GetSampleTeamId()
    {
        return _sampleTeamId;
    }

    public static Guid GetSamplePlayerId()
    {
        return _samplePlayerId;
    }

    public static Guid GetSampleSeasonId()
    {
        return _sampleSeasonId;
    }

    public static Guid GetSampleStadiumId()
    {
        return _sampleStadiumId;
    }

    public static Guid GetSampleCoachId()
    {
        return _sampleCoachId;
    }

    public static Team CreateTeam(string name)
    {
        return new Team
        {
            Id = 1,
            Name = name,
            Country = "Testland",
            FoundationDate = new DateTime(1900, 1, 1),
        };
    }

    public static Player CreatePlayer(string player, int teamId)
    {
        return new Player
        {
            Id = 0,
            FullName = player,
            TeamId = teamId,
        };
    }

    public static Coach CreateCoach(string headCoach, int teamId)
    {
        return new Coach
        {
            Id = 0,
            FirstName = headCoach,
            TeamId = teamId,
        };
    }

    public static Team CreateTestTeam()
    {
        return new Team
        {
            Id = 0,
            Name = "Test Team",
            ShortName = "TT",
            Country = "Testland",
            FoundationDate = new DateTime(1900, 1, 1),
        };
    }

    public static Season CreateTestSeason()
    {
        return new Season
        {
            Id = 0,
            Name = "Test Season",
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2024, 1, 1),
            LeagueName = "Test League",
            Country = "Testland",
        };
    }

    public static Stadium CreateTestStadium()
    {
        return new Stadium
        {
            Id = 0,
            Name = "Test Stadium",
            Capacity = 50000,
            Country = "Testland",
        };
    }

    public static TeamSeasons CreateTestSeasonTeam(int seasonId, int homeTeamId)
    {
        return new TeamSeasons
        {
            Id = 0,
            SeasonId = seasonId,
            TeamId = homeTeamId,
        };
    }

    public static Coach CreateTestCoach(int homeTeamId)
    {
        return new Coach
        {
            Id = 0,
            FirstName = "Test",
            LastName = "Coach",
            TeamId = homeTeamId,
        };
    }

    public static Match CreateTestMatch(int homeTeamId, int awayTeamId)
    {
        return new Match
        {
            Id = 0,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            ScheduledDateTimeUtc = DateTime.UtcNow,
            StadiumId = 0,
            MatchWeek = null,
            HomeCoachId = null,
            AwayCoachId = null,
            HomeTeamScore = null,
            AwayTeamScore = null,
            WinningTeamId = null,
            LosingTeamId = null,
            IsDraw = null, // Assuming stadium is not set for this test match
            HomeTeamSeasonId = 0,
            HomeTeamSeason = null, // Assuming season is not set for this test match
            AwayTeamSeasonId = 0,
            AwayTeamSeason = null,
            HomeTeamInMatchName = null,
            AwayTeamInMatchName = null, // Assuming season is not set for this test match
            MatchStatus = "Scheduled",
            ModelSimulationStartTimeUtc = null,
            HomeTeamPossession = null,
            AwayTeamPossession = null,
            HomeTeamShots = null,
            AwayTeamShots = null,
            HomeTeamShotsOnTarget = null,
            AwayTeamShotsOnTarget = null,
            HomeTeamCorners = null,
            AwayTeamCorners = null,
            HomeTeamFouls = null,
            AwayTeamFouls = null,
            HomeTeamYellowCards = null,
            AwayTeamYellowCards = null,
            HomeTeamRedCards = null,
            AwayTeamRedCards = null,
            HomeTeamOffsides = null,
            AwayTeamOffsides = null,
            HomeTeamPasses = null,
            HomeTeamPassesCompleted = null,
            AwayTeamPassesCompleted = null,
            AwayTeamPasses = null,
            HomeTeamPassAccuracy = null,
            AwayTeamPassAccuracy = null,
            HomeTeamPossessionDurationSeconds = null,
            AwayTeamPossessionDurationSeconds = null,
            LastEventTimestampSeconds = null,
            LastEventPossessingTeamName = null,
            HomeTeamDribbles = null,
            AwayTeamDribbles = null,
            HomeTeamSaves = null,
            AwayTeamSaves = null,
            HomeTeamDuels = null,
            AwayTeamDuels = null,
            HomeTeamDuelsWon = null,
            AwayTeamDuelsWon = null,
            HomeTeamClearances = null,
            AwayTeamClearances = null,
            HomeTeamPossessionWon = null,
            AwayTeamPossessionWon = null,
            HomeTeamRecoveries = null,
            AwayTeamRecoveries = null,
            HomeTeamGoalKicks = null,
            AwayTeamGoalKicks = null,
            HomeLongBalls = null,
            AwayLongBalls = null,
            HomeAccurateLongBalls = null,
            AwayAccurateLongBalls = null,
            HomeTeamLongBallsAccuracy = null,
            AwayTeamLongBallsAccuracy = null,
            HomeTeamFreeKicks = null,
            AwayTeamFreeKicks = null,
            AwayTeamShotsOffTarget = null,
            HomeTeamShotsOffTarget = null,
            IsLive = false,
            CreatedAt = default,
            UpdatedAt = default,
            HomeTeam = null,
            AwayTeam = null,
            Stadium = null,
            HomeCoach = null,
            AwayCoach = null,
            CreatorId = "test_creator",
            SimulationId = null,
            MatchEvents = null,
        };
    }

    public static Player CreateTestPlayer(int teamId)
    {
        return new Player
        {
            Id = 0,
            FullName = "Test Player",
            KnownName = "TP",
            Nationality = "Testland",
            ShirtNumber = 8,
            PreferredFoot = "Right",
            TeamId = 0,
            PhotoUrl = "http://example.com/photo.jpg",
            Position = nameof(PlayerPosition.CentralMidfielder),
        };
    }

    public static async Task SeedTestData(IServiceProvider scopeServiceProvider)
    {
        using var scope = scopeServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Create and add teams
        var team1 = CreateTeam("Test Team 1");
        var team2 = CreateTeam("Test Team 2");
        dbContext.Teams.AddRange(team1, team2);

        // Create and add players
        var player1 = CreatePlayer("Test Player 1", team1.Id);
        var player2 = CreatePlayer("Test Player 2", team2.Id);
        dbContext.Players.AddRange(player1, player2);

        // Create and add coaches
        var coach1 = CreateCoach("Head Coach 1", team1.Id);
        var coach2 = CreateCoach("Head Coach 2", team2.Id);
        dbContext.Coaches.AddRange(coach1, coach2);

        // Create and add seasons
        var season = CreateTestSeason();
        dbContext.Seasons.Add(season);

        // Create and add stadiums
        var stadium = CreateTestStadium();
        dbContext.Stadiums.Add(stadium);

        // Save changes to the database
        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedPlayersWithNationality(
        IServiceProvider scopeServiceProvider,
        string testNationality,
        int count
    )
    {
        using var scope = scopeServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Create players
        var team = CreateTestTeam();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < count; i++)
        {
            var player = new Player
            {
                Id = 0,
                FullName = $"Test Player {i + 1}",
                KnownName = $"TP{i + 1}",
                Nationality = testNationality,
                ShirtNumber = i + 1,
                PreferredFoot = i % 2 == 0 ? "Right" : "Left",
                TeamId = team.Id,
                Position = ((PlayerPosition)(i % 4 + 1)).ToString(),
            };
            dbContext.Players.Add(player);
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedPlayersWithPreferredFoot(
        IServiceProvider scopeServiceProvider,
        string testPreferredFoot,
        int count
    )
    {
        using var scope = scopeServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Create team
        var team = CreateTestTeam();
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < count; i++)
        {
            var player = new Player
            {
                Id = 0,
                FullName = $"Test Player {i + 1}",
                KnownName = $"TP{i + 1}",
                Nationality = "Testland",
                ShirtNumber = i + 1,
                PreferredFoot = testPreferredFoot,
                TeamId = team.Id,
                Position = ((PlayerPosition)(i % 4 + 1)).ToString(),
            };
            dbContext.Players.Add(player);
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedPlayersForTeam(
        IServiceProvider scopeServiceProvider,
        int testTeamId,
        int count
    )
    {
        using var scope = scopeServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        for (var i = 0; i < count; i++)
        {
            var player = new Player
            {
                Id = 0,
                FullName = $"Test Player {i + 1}",
                KnownName = $"TP{i + 1}",
                Nationality = "Testland",
                ShirtNumber = i + 1,
                PreferredFoot = i % 2 == 0 ? "Right" : "Left",
                TeamId = testTeamId,
                Position = ((PlayerPosition)(i % 4 + 1)).ToString(),
            };
            dbContext.Players.Add(player);
        }

        await dbContext.SaveChangesAsync();
    }

    public static Notification CreateTestNotification(string userId)
    {
        return new Notification
        {
            Id = Guid.Empty.ToString(),
            Content = "Test Notification",
            UserId = userId,
            Type = NotificationType.MatchUpdate,
            Time = default,
            IsRead = false,
            Title = "Test Match Update",
        };
    }

    public static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            PhoneNumber = "1234567890",
            PhoneNumberConfirmed = true,
            FirstName = "Test",
            LastName = "User",
        };
    }

    public static Season CreateSeason(string seasonName, string leagueName = "Test League")
    {
        return new Season
        {
            Id = 0,
            Name = seasonName,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(9),
            LeagueName = leagueName,
            Country = "Testland",
            IsActive = true,
        };
    }

    public static Stadium CreateStadium(string StadiumName)
    {
        return new Stadium
        {
            Id = 0,
            Name = StadiumName,
            City = "Test City",
            Country = "Testland",
            Capacity = 50000,
            BuiltDate = null,
            Description = null,
            ImageUrl = null,
            SurfaceType = null,
        };
    }
}
