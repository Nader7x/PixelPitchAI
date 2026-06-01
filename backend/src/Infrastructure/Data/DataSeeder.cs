using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Dapper;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public class DataSeeder(FootballDbContext context, ILogger<DataSeeder> logger)
{
    private readonly string _dataFolderPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Data")
    );

    public async Task SeedAllAsync()
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedAsync("Stadiums.csv", context.Stadiums);
            await ResetSequenceAsync("Stadiums", "Id");
            await SeedAsync("Competitions.csv", context.Competitions);
            await ResetSequenceAsync("Competitions", "Id");
            await SeedAsync("Teams.csv", context.Teams);
            await ResetSequenceAsync("Teams", "Id");
            await SeedAsync("Coaches.csv", context.Coaches);
            await ResetSequenceAsync("Coaches", "Id");
            await SeedAsync("Players.csv", context.Players);
            await ResetSequenceAsync("Players", "Id");
            await SeedAsync("Seasons.csv", context.Seasons);
            await ResetSequenceAsync("Seasons", "Id");
            await SeedAsync("TeamSeasons.csv", context.TeamSeasons);
            await ResetSequenceAsync("TeamSeasons", "Id");
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Seeding Failed and transaction rolled back.");
        }
    }

    private async Task SeedAsync<T>(string fileName, DbSet<T> dbSet)
        where T : class
    {
        var filePath = Path.Combine(_dataFolderPath, fileName);
        if (!File.Exists(filePath))
        {
            logger.LogWarning("CSV file not found: {FilePath}", filePath);
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            csv.Context.TypeConverterCache.AddConverter<bool>(new CustomBooleanConverter());
            csv.Context.TypeConverterCache.AddConverter<DateTime>(new UtcDateTimeConverter());

            if (typeof(T) == typeof(Team))
                csv.Context.RegisterClassMap<TeamMap>();
            else if (typeof(T) == typeof(Stadium))
                csv.Context.RegisterClassMap<StadiumMap>();
            else if (typeof(T) == typeof(Competition))
                csv.Context.RegisterClassMap<CompetitionMap>();
            else if (typeof(T) == typeof(Coach))
                csv.Context.RegisterClassMap<CoachMap>();
            else if (typeof(T) == typeof(Player))
                csv.Context.RegisterClassMap<PlayerMap>();
            else if (typeof(T) == typeof(Season))
                csv.Context.RegisterClassMap<SeasonMap>();
            else if (typeof(T) == typeof(TeamSeason))
                csv.Context.RegisterClassMap<TeamSeasonsMap>();

            if (await dbSet.AnyAsync())
            {
                logger.LogInformation(
                    "{TableName} data already exists. Skipping seeding.",
                    typeof(T).Name
                );
                return;
            }

            List<T> recordsToProcess;

            if (typeof(T) == typeof(Team))
            {
                var teamsToSeed = csv.GetRecords<Team>().ToList();
                recordsToProcess = teamsToSeed.Cast<T>().ToList();
            }
            else if (typeof(T) == typeof(Coach))
            {
                var coachesToSeed = csv.GetRecords<Coach>().ToList();
                foreach (var coach in coachesToSeed)
                {
                    if (coach.TeamId.HasValue)
                    {
                        var teamExists = await context.Teams.AnyAsync(t =>
                            t.Id == coach.TeamId.Value
                        );
                        if (!teamExists)
                        {
                            logger.LogWarning(
                                "Team with Id {TeamId} not found for Coach {CoachName}. Relationship will not be established.",
                                coach.TeamId.Value,
                                $"{coach.FirstName} {coach.LastName}"
                            );
                            coach.TeamId = null;
                        }
                    }
                }
                recordsToProcess = coachesToSeed.Cast<T>().ToList();
            }
            else if (typeof(T) == typeof(Player))
            {
                var playersToSeed = csv.GetRecords<Player>().ToList();
                foreach (var player in playersToSeed)
                {
                    if (player.TeamId.HasValue)
                    {
                        var teamExists = await context.Teams.AnyAsync(t =>
                            t.Id == player.TeamId.Value
                        );
                        if (!teamExists)
                        {
                            logger.LogWarning(
                                "Team with Id {TeamId} not found for Player {PlayerName}. Relationship will not be established.",
                                player.TeamId.Value,
                                player.FullName
                            );
                            player.TeamId = null;
                        }
                    }
                }
                recordsToProcess = playersToSeed.Cast<T>().ToList();
            }
            else if (typeof(T) == typeof(Season))
            {
                var seasonsToSeed = csv.GetRecords<Season>().ToList();
                foreach (var season in seasonsToSeed)
                {
                    var competitionExists = await context.Competitions.AnyAsync(c =>
                        c.Id == season.CompetitionId
                    );
                    if (!competitionExists)
                    {
                        logger.LogWarning(
                            "Competition with Id {CompetitionId} not found for Season {SeasonName}. Relationship will not be established.",
                            season.CompetitionId,
                            season.Name
                        );
                    }
                }
                recordsToProcess = seasonsToSeed.Cast<T>().ToList();
            }
            else if (typeof(T) == typeof(TeamSeason))
            {
                var teamSeasonsToSeed = csv.GetRecords<TeamSeason>().ToList();
                List<TeamSeason> validTeamSeasons = new List<TeamSeason>();

                foreach (var teamSeason in teamSeasonsToSeed)
                {
                    bool teamFound = await context.Teams.AnyAsync(t => t.Id == teamSeason.TeamId);
                    bool seasonFound = await context.Seasons.AnyAsync(s =>
                        s.Id == teamSeason.SeasonId
                    );

                    if (teamFound && seasonFound)
                    {
                        validTeamSeasons.Add(teamSeason);
                    }
                    else
                    {
                        if (!teamFound)
                        {
                            logger.LogError(
                                "Team with Id {TeamId} not found for TeamSeason (SeasonId: {SeasonId}). Skipping record.",
                                teamSeason.TeamId,
                                teamSeason.SeasonId
                            );
                        }
                        if (!seasonFound)
                        {
                            logger.LogError(
                                "Season with Id {SeasonId} not found for TeamSeason (TeamId: {TeamId}). Skipping record.",
                                teamSeason.SeasonId,
                                teamSeason.TeamId
                            );
                        }
                    }
                }
                recordsToProcess = validTeamSeasons.Cast<T>().ToList();
            }
            else
            {
                recordsToProcess = csv.GetRecords<T>().ToList();
            }

            logger.LogInformation(
                "Seeding {Count} records for {TableName}.",
                recordsToProcess.Count,
                typeof(T).Name
            );

            await dbSet.AddRangeAsync(recordsToProcess);
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Seeded {Count} records into {TableName}.",
                recordsToProcess.Count,
                typeof(T).Name
            );
        }
        catch (Exception ex)
        {
            var innerMostException = ex;
            while (innerMostException.InnerException != null)
            {
                innerMostException = innerMostException.InnerException;
            }
            logger.LogError(
                ex,
                "Error occurred while seeding data from {FileName}: {Message}",
                fileName,
                innerMostException.Message
            );
        }
    }

    private sealed class StadiumMap : ClassMap<Stadium>
    {
        public StadiumMap()
        {
            Map(s => s.Id).Name("Id");
            Map(s => s.Name).Name("Name");
            Map(s => s.Capacity).Name("Capacity");
            Map(s => s.BuiltDate).Name("BuiltDate");
            Map(s => s.LastRenovation).Name("LastRenovation");
            Map(s => s.SurfaceType).Name("SurfaceType");
            Map(s => s.Address).Name("Address");
            Map(s => s.Latitude).Name("Latitude");
            Map(s => s.Longitude).Name("Longitude");
            Map(s => s.HasRoof).Name("HasRoof");
            Map(s => s.ImageUrl).Name("ImageUrl");
            Map(s => s.Description).Name("Description");
            Map(s => s.Facilities).Name("Facilities");
            Map(s => s.Architect).Name("Architect");
            Map(s => s.CostMillionsEuros).Name("CostMillionsEuros");
            Map(s => s.Nickname).Name("Nickname");
        }
    }

    private sealed class TeamMap : ClassMap<Team>
    {
        public TeamMap()
        {
            Map(t => t.Id).Name("Id");
            Map(t => t.Name).Name("Name");
            Map(t => t.ShortName).Name("ShortName");
            Map(t => t.FoundationDate).Name("FoundationDate");
            Map(t => t.PrimaryColor).Name("PrimaryColor");
            Map(t => t.SecondaryColor).Name("SecondaryColor");
            Map(t => t.Logo).Name("Logo");
            Map(t => t.City).Name("City");
            Map(t => t.Country).Name("Country");
            Map(t => t.League).Name("League");
            Map(t => t.StadiumId).Name("StadiumId");
        }
    }

    private sealed class CompetitionMap : ClassMap<Competition>
    {
        public CompetitionMap()
        {
            Map(c => c.Id).Name("Id");
            Map(c => c.Name).Name("Name");
            Map(c => c.Description).Name("Description");
            Map(c => c.Country).Name("Country");
            Map(c => c.Logo).Name("Logo");
        }
    }

    private sealed class CoachMap : ClassMap<Coach>
    {
        public CoachMap()
        {
            Map(c => c.Id).Name("Id");
            Map(c => c.FirstName).Name("FirstName");
            Map(c => c.LastName).Name("LastName");
            Map(c => c.DateOfBirth).Name("DateOfBirth");
            Map(c => c.Nationality).Name("Nationality");
            Map(c => c.Role).Name("Role");
            Map(c => c.YearsOfExperience).Name("YearsOfExperience");
            Map(c => c.PhotoUrl).Name("PhotoUrl");
            Map(c => c.Biography).Name("Biography");
            Map(c => c.TeamId).Name("TeamId");
            Map(c => c.CoachingStyle).Name("CoachingStyle");
            Map(c => c.PreferredFormation).Name("PreferredFormation");
        }
    }

    private sealed class PlayerMap : ClassMap<Player>
    {
        public PlayerMap()
        {
            Map(p => p.Id).Name("Id");
            Map(p => p.FullName).Name("FullName");
            Map(p => p.KnownName).Name("KnownName");
            Map(p => p.Nationality).Name("Nationality");
            Map(p => p.ShirtNumber).Name("ShirtNumber");
            Map(p => p.PreferredFoot).Name("PreferredFoot");
            Map(p => p.TeamId).Name("TeamId");
            Map(p => p.PhotoUrl).Name("PhotoUrl");
            Map(p => p.CreatedAt).Name("CreatedAt");
            Map(p => p.UpdatedAt).Name("UpdatedAt");
            Map(p => p.Position).Name("Position");
        }
    }

    private sealed class SeasonMap : ClassMap<Season>
    {
        public SeasonMap()
        {
            Map(s => s.Id).Name("Id");
            Map(s => s.Name).Name("Name");
            Map(s => s.IsActive).Name("IsActive");
            Map(s => s.IsCompleted).Name("IsCompleted");
            Map(s => s.LeagueName).Name("LeagueName");
            Map(s => s.Country).Name("Country");
            Map(s => s.TotalRounds).Name("TotalRounds");
            Map(s => s.CurrentRound).Name("CurrentRound");
            Map(s => s.CreatedAt).Name("CreatedAt");
            Map(s => s.UpdatedAt).Name("UpdatedAt");
            Map(s => s.EndDate).Name("EndDate");
            Map(s => s.StartDate).Name("StartDate");
            Map(s => s.CompetitionId).Name("CompetitionId");
        }
    }

    private sealed class TeamSeasonsMap : ClassMap<TeamSeason>
    {
        public TeamSeasonsMap()
        {
            Map(ts => ts.Id).Name("Id");
            Map(ts => ts.TeamId).Name("TeamId");
            Map(ts => ts.SeasonId).Name("SeasonId");
            Map(ts => ts.UpdatedAt).Name("UpdatedAt");
        }
    }

    private sealed class CustomBooleanConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(
            string? text,
            IReaderRow row,
            MemberMapData memberMapData
        )
        {
            if (
                string.Equals(text, "t", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
            )
                return true;
            if (
                string.Equals(text, "f", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)
            )
                return false;

            return base.ConvertFromString(text, row, memberMapData);
        }
    }

    private sealed class UtcDateTimeConverter : DateTimeConverter
    {
        public override object? ConvertFromString(
            string? text,
            IReaderRow row,
            MemberMapData memberMapData
        )
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var dateTime = (DateTime?)base.ConvertFromString(text, row, memberMapData);
            return dateTime.HasValue
                ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc)
                : null;
        }
    }

    private async Task ResetSequenceAsync(string tableName, string idColumn)
    {
        var quotedTableName = $"\"public\".\"{tableName}\"";
        var quotedIdColumn = $"\"{idColumn}\"";

        try
        {
            // Get the connection from the DbContext BUT DO NOT dispose of it with 'using'.
            // The DbContext owns the connection's lifetime.
            var connection = context.Database.GetDbConnection();

            // Ensure the connection is open before Dapper uses it.
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await context.Database.OpenConnectionAsync();
            }

            var sequenceQuery = $"SELECT pg_get_serial_sequence('{quotedTableName}', '{idColumn}')";
            var sequenceName = await connection.QuerySingleOrDefaultAsync<string>(sequenceQuery);

            if (string.IsNullOrEmpty(sequenceName))
            {
                logger.LogWarning(
                    "Could not find sequence for table {TableName}. Skipping reset.",
                    tableName
                );
                return;
            }

            var resetQuery =
                $@"
            SELECT setval(@SequenceName,
            COALESCE((SELECT MAX({quotedIdColumn}) FROM {quotedTableName}), 0) + 1, false);";

            await connection.ExecuteAsync(resetQuery, new { SequenceName = sequenceName });

            logger.LogInformation(
                "Successfully reset sequence {SequenceName} for table {TableName}.",
                sequenceName,
                tableName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset sequence for table {TableName}.", tableName);
        }
    }
}
