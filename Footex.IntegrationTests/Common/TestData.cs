using Dapper;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Footex.IntegrationTests.Common;

public static class TestData
{
    public static Competition CreateTestCompetition()
    {
        return new Competition
        {
            Name = "Test Competition" + Guid.NewGuid(),
            Description = "Competitions Description",
            Country = "Competition Country",
        };
    }

    public static Team CreateTeam(string name)
    {
        return new Team
        {
            Name = name,
            Country = "Testland",
            FoundationDate = new DateTime(1900, 1, 1),
        };
    }

    public static Team CreateTestTeam(string name = "Test Team")
    {
        return new Team
        {
            Id = 0,
            Name = name + " " + Guid.NewGuid(),
            ShortName = "TT",
            Country = "Testland",
            FoundationDate = new DateTime(1900, 1, 1),
        };
    }

    public static Team CreateTestDbTeam(string prefix = "", string name = "")
    {
        return new Team
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Test Team {Guid.NewGuid()} {prefix}" : name,
            ShortName =
                $"TT{Guid.NewGuid().ToString()[..5]} {(!prefix.IsNullOrEmpty() ? prefix.First() : prefix)}",
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

    public static Season CreateTestDbSeason()
    {
        return new Season
        {
            Name = "Test Season " + Guid.NewGuid(),
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2024, 1, 1),
            LeagueName = "Premier League",
            Country = "England",
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

    public static Stadium CreateTestDbStadium(string name = "Test Stadium")
    {
        return new Stadium
        {
            Name = name + Guid.NewGuid(),
            Capacity = 50000,
            Country = "England",
            City = "London",
        };
    }

    public static TeamSeason CreateTestSeasonTeam(int seasonId, int homeTeamId)
    {
        return new TeamSeason
        {
            Id = 0,
            SeasonId = seasonId,
            TeamId = homeTeamId,
        };
    }

    public static TeamSeason CreateTestDbSeasonTeam(int seasonId, int teamId)
    {
        return new TeamSeason { SeasonId = seasonId, TeamId = teamId };
    }

    public static Coach CreateTestCoach(int teamId)
    {
        return new Coach
        {
            FirstName = "Test",
            LastName = "Coach",
            Nationality = "TestLand",
            DateOfBirth = new DateTime(),
            YearsOfExperience = 5,
            Biography = "bio",
            Role = "Head Coach",
            TeamId = teamId,
        };
    }

    public static Coach CreateTestDbCoach(
        int teamId = 0,
        string firstName = "Test",
        string lastName = "Coach"
    )
    {
        if (teamId > 0)
            return new Coach
            {
                FirstName = firstName == "Test" ? "Test" + " " + Guid.NewGuid() : firstName,
                LastName = lastName == "Coach" ? "Coach" + " " + Guid.NewGuid() : lastName,
                TeamId = teamId,
                DateOfBirth = new DateTime(1980, 1, 1),
                Nationality = "England",
                YearsOfExperience = 10,
                Role = "Head Coach",
                PreferredFormation = "4-3-3",
            };
        return new Coach
        {
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = new DateTime(1980, 1, 1),
            Nationality = "England",
            YearsOfExperience = 10,
            Role = "Head Coach",
            PreferredFormation = "4-3-3",
        };
    }

    public static Match CreateTestMatch(
        int homeTeamId,
        int awayTeamId,
        int seasonId,
        bool db = false,
        string creatorId = ""
    )
    {
        if (db)
            return new Match
            {
                HomeTeamId = homeTeamId,
                AwayTeamId = awayTeamId,
                ScheduledDateTimeUtc = DateTime.UtcNow,
                CreatorId = creatorId,
                HomeTeamSeasonId = seasonId,
                AwayTeamSeasonId = seasonId,
                MatchWeek = 1,
            };
        return new Match
        {
            Id = 0,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            ScheduledDateTimeUtc = DateTime.UtcNow,
            StadiumId = 1,
            MatchWeek = null,
            HomeCoachId = null,
            AwayCoachId = null,
            HomeTeamScore = null,
            AwayTeamScore = null,
            WinningTeamId = null,
            LosingTeamId = null,
            IsDraw = null, // Assuming stadium is not set for this test match
            HomeTeamSeasonId = 1,
            HomeTeamSeason = null, // Assuming season is not set for this test match
            AwayTeamSeasonId = 2,
            AwayTeamSeason = null,
            HomeTeamInMatchName = null,
            AwayTeamInMatchName = null, // Assuming season is not set for this test match
            MatchStatus = "Scheduled",
            ModelSimulationStartTimeUtc = null,
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

    public static Player CreateTestPlayer(int teamId = 0)
    {
        return new Player
        {
            FullName = "Test Player",
            KnownName = "TP",
            Nationality = "England",
            ShirtNumber = 10,
            PreferredFoot = "Right",
            TeamId = teamId,
            Position = nameof(PlayerPosition.CentralMidfielder),
        };
    }

    public static Player CreateTestDbPlayer(int teamId = 0)
    {
        if (teamId > 0)
            return new Player
            {
                FullName = "Test Player" + Guid.NewGuid(),
                KnownName = "TP",
                Nationality = "England",
                ShirtNumber = 10,
                PreferredFoot = "Right",
                TeamId = teamId,
                Position = nameof(PlayerPosition.CentralMidfielder),
                PhotoUrl = "www.photourl.example.com",
            };
        return new Player
        {
            FullName = "Test Player",
            KnownName = "TP",
            Nationality = "England",
            ShirtNumber = 10,
            PreferredFoot = "Right",
            Position = nameof(PlayerPosition.CentralMidfielder),
        };
    }

    public static async Task SeedTestData(IServiceProvider scopeServiceProvider)
    {
        using var scope = scopeServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Create and add teams
        var team1 = CreateTestDbTeam();
        var team2 = CreateTestDbTeam();
        await dbContext.Teams.AddRangeAsync(team1, team2);
        await dbContext.SaveChangesAsync();

        // Create and add players
        var player1 = CreateTestDbPlayer(team1.Id);
        var player2 = CreateTestDbPlayer(team2.Id);
        await dbContext.Players.AddRangeAsync(player1, player2);
        await dbContext.SaveChangesAsync();

        // Create and add coaches
        var coach1 = CreateTestDbCoach(team1.Id);
        var coach2 = CreateTestDbCoach(team2.Id);
        await dbContext.Coaches.AddRangeAsync(coach1, coach2);

        // Create and add seasons
        var season = CreateTestDbSeason();
        await dbContext.Seasons.AddAsync(season);

        // Create and add stadiums
        var stadium = CreateTestDbStadium();
        await dbContext.Stadiums.AddAsync(stadium);

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
        var team = CreateTestDbTeam();
        await dbContext.Teams.AddAsync(team);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < count; i++)
        {
            var player = new Player
            {
                FullName = $"Test Player {i + 1}",
                KnownName = $"TP{i + 1}",
                Nationality = testNationality,
                ShirtNumber = i + 1,
                PreferredFoot = i % 2 == 0 ? "Right" : "Left",
                TeamId = team.Id,
                Position = ((PlayerPosition)(i % 4 + 1)).ToString(),
            };
            await dbContext.Players.AddAsync(player);
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
        var team = CreateTestDbTeam();
        await dbContext.Teams.AddAsync(team);
        await dbContext.SaveChangesAsync();

        for (var i = 0; i < count; i++)
        {
            var player = new Player
            {
                FullName = $"Test Player {i + 1}",
                KnownName = $"TP{i + 1}",
                Nationality = "Testland",
                ShirtNumber = i + 1,
                PreferredFoot = testPreferredFoot,
                TeamId = team.Id,
                Position = ((PlayerPosition)(i % 4 + 1)).ToString(),
            };
            await dbContext.Players.AddAsync(player);
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
            Content = "Test Notification",
            UserId = userId,
            Type = NotificationType.MatchUpdate,
            Time = default,
            IsRead = false,
            Title = "Test Match Update",
        };
    }

    public static ApplicationUser CreateTestUser(bool db = false)
    {
        return new ApplicationUser
        {
            Id = db ? Guid.NewGuid().ToString() : "test_user_id",
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

    public static Stadium CreateStadium(string stadiumName)
    {
        return new Stadium
        {
            Id = 0,
            Name = stadiumName,
            City = "Test City",
            Country = "Testland",
            Capacity = 50000,
            BuiltDate = null,
            Description = null,
            ImageUrl = null,
            SurfaceType = null,
        };
    }

    /// <summary>
    /// Defines what and how to seed.
    /// </summary>
    public class SeedingOptions
    {
        public int StadiumsToCreate { get; set; } = 1;
        public int TeamsToCreate { get; set; } = 1;
        public int PlayersPerTeam { get; set; } = 1;
        public int CoachesPerTeam { get; set; } = 1;
        public int SeasonsToCreate { get; set; } = 1;
        public bool ClearDatabase { get; set; } = true;
    }

    /// <summary>
    /// Seeds a single instance of each primary entity and returns them.
    /// Ideal for simple tests requiring one of everything.
    /// </summary>
    public static async Task<(
        Team Team,
        Player Player,
        Coach Coach,
        Season Season,
        Stadium Stadium
    )> SeedAndGetSingleAsync(IServiceProvider serviceProvider)
    {
        var options = new SeedingOptions
        {
            StadiumsToCreate = 1,
            TeamsToCreate = 1,
            PlayersPerTeam = 1,
            CoachesPerTeam = 1,
            SeasonsToCreate = 1,
            ClearDatabase = false,
        };

        var (teams, players, coaches, seasons, stadiums) = await SeedAndGetMultipleAsync(
            serviceProvider,
            options
        );

        return (teams.First(), players.First(), coaches.First(), seasons.First(), stadiums.First());
    }

    /// <summary>
    /// Seeds a configurable number of entities and returns them in lists.
    /// Ideal for complex tests requiring multiple entities.
    /// </summary>
    public static async Task<(
        List<Team> Teams,
        List<Player> Players,
        List<Coach> Coaches,
        List<Season> Seasons,
        List<Stadium> Stadiums
    )> SeedAndGetMultipleAsync(IServiceProvider serviceProvider, SeedingOptions options)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        if (options.ClearDatabase)
        {
            var connection = dbContext.Database.GetDbConnection();
            // Truncate tables for a clean slate. Order is important due to foreign keys.
            await connection.ExecuteAsync(
                @"TRUNCATE TABLE ""Players"", ""Coaches"", ""TeamSeasons"", ""Teams"", ""Seasons"", ""Stadiums"", ""Competitions"" RESTART IDENTITY"
            );
        }

        var createdStadiums = new List<Stadium>();
        var createdSeasons = new List<Season>();
        var createdTeams = new List<Team>();
        var createdPlayers = new List<Player>();
        var createdCoaches = new List<Coach>();

        // 1. Create entities that don't depend on others
        for (var i = 0; i < options.StadiumsToCreate; i++)
        {
            createdStadiums.Add(CreateTestDbStadium());
        }
        await dbContext.Stadiums.AddRangeAsync(createdStadiums);

        for (var i = 0; i < options.SeasonsToCreate; i++)
        {
            createdSeasons.Add(CreateTestDbSeason());
        }
        await dbContext.Seasons.AddRangeAsync(createdSeasons);
        await dbContext.SaveChangesAsync(); // Save to get IDs

        // 2. Create Teams and their dependent entities (Players, Coaches)
        for (var i = 0; i < options.TeamsToCreate; i++)
        {
            var stadium = createdStadiums.Count != 0;
            var team = CreateTestDbTeam();
            createdTeams.Add(team);
            await dbContext.Teams.AddAsync(team);
            await dbContext.SaveChangesAsync(); // Save to get Team ID

            for (var p = 0; p < options.PlayersPerTeam; p++)
            {
                createdPlayers.Add(CreateTestDbPlayer(team.Id));
            }

            for (var c = 0; c < options.CoachesPerTeam; c++)
            {
                createdCoaches.Add(CreateTestDbCoach(team.Id));
            }
        }

        await dbContext.Players.AddRangeAsync(createdPlayers);
        await dbContext.Coaches.AddRangeAsync(createdCoaches);

        // 3. Final save for all remaining entities
        await dbContext.SaveChangesAsync();

        return (createdTeams, createdPlayers, createdCoaches, createdSeasons, createdStadiums);
    }
}
