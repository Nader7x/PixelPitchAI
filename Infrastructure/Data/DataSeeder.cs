using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly FootballDbContext _context;
        private readonly ILogger<DataSeeder> _logger;
        private readonly string _dataFolderPath;

        public DataSeeder(FootballDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
            _dataFolderPath = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Infrastructure",
                    "Data"
                )
            );
        }

        public async Task SeedAllAsync()
        {
            await SeedAsync<Stadium>("Stadiums.csv", _context.Stadiums);
            await SeedAsync<Competition>("Competitions.csv", _context.Competitions);
            await SeedAsync<Team>("Teams.csv", _context.Teams);
            await SeedAsync<Coach>("Coaches.csv", _context.Coaches);
            await SeedAsync<Player>("Players.csv", _context.Players);
            await SeedAsync<Season>("Seasons.csv", _context.Seasons);
            await SeedAsync<TeamSeasons>("TeamSeasons.csv", _context.TeamSeasons);
        }

        private async Task SeedAsync<T>(string fileName, DbSet<T> dbSet)
            where T : class
        {
            string filePath = Path.Combine(_dataFolderPath, fileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("CSV file not found: {FilePath}", filePath);
                return;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null, // Suppress warnings for missing fields
                BadDataFound = null, // Suppress warnings for bad data
            };

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    // Register custom converters
                    csv.Context.TypeConverterCache.AddConverter<bool>(new CustomBooleanConverter());
                    csv.Context.TypeConverterCache.AddConverter<DateTime>(
                        new UtcDateTimeConverter()
                    );

                    // Register the ClassMap for the current type T
                    if (typeof(T) == typeof(Team))
                    {
                        csv.Context.RegisterClassMap<TeamMap>();
                    }
                    else if (typeof(T) == typeof(Stadium))
                    {
                        csv.Context.RegisterClassMap<StadiumMap>();
                    }
                    else if (typeof(T) == typeof(Competition))
                    {
                        csv.Context.RegisterClassMap<CompetitionMap>();
                    }
                    else if (typeof(T) == typeof(Coach))
                    {
                        csv.Context.RegisterClassMap<CoachMap>();
                    }
                    else if (typeof(T) == typeof(Player))
                    {
                        csv.Context.RegisterClassMap<PlayerMap>();
                    }
                    else if (typeof(T) == typeof(Season))
                    {
                        csv.Context.RegisterClassMap<SeasonMap>();
                    }
                    else if (typeof(T) == typeof(TeamSeasons))
                    {
                        csv.Context.RegisterClassMap<TeamSeasonsMap>();
                    }

                    _logger.LogInformation(
                        "checking if the current table has data : {Count}",
                        await dbSet.CountAsync()
                    );

                    if (await dbSet.AnyAsync())
                    {
                        _logger.LogInformation(
                            "{TableName} data already exists. Skipping seeding.",
                            typeof(T).Name
                        );
                        return;
                    }

                    List<T> recordsToProcess; // Declare the list here

                    // Special handling for Teams to ensure Stadium navigation property is not populated
                    if (typeof(T) == typeof(Team))
                    {
                        var teamsToSeed = csv.GetRecords<Team>().ToList();
                        foreach (var team in teamsToSeed)
                        {
                            // Crucial: Ensure the Stadium navigation property is null
                            // This prevents EF Core from trying to insert a new Stadium entity
                            team.Stadium = null;
                        }
                        recordsToProcess = teamsToSeed.Cast<T>().ToList(); // Assign to the common variable
                    }
                    else
                    {
                        recordsToProcess = csv.GetRecords<T>().ToList(); // Assign to the common variable
                    }

                    _logger.LogInformation(
                        "Seeding {Count} records for {TableName}.",
                        recordsToProcess.Count,
                        typeof(T).Name
                    );

                    await dbSet.AddRangeAsync(recordsToProcess); // Use the common variable
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Seeded {Count} records into {TableName}.",
                        recordsToProcess.Count,
                        typeof(T).Name
                    ); // Use the common variable
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while seeding data from {FileName}: {Message}",
                    fileName,
                    ex.Message
                );
            }
        }

        public class StadiumMap : ClassMap<Stadium>
        {
            public StadiumMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
                Map(m => m.Capacity).Name("Capacity");
                Map(m => m.BuiltDate).Name("BuiltDate");
                Map(m => m.LastRenovation).Name("LastRenovation");
                Map(m => m.SurfaceType).Name("SurfaceType");
                Map(m => m.Address).Name("Address");
                Map(m => m.Latitude).Name("Latitude");
                Map(m => m.Longitude).Name("Longitude");
                Map(m => m.HasRoof).Name("HasRoof");
                Map(m => m.ImageUrl).Name("ImageUrl");
                Map(m => m.Description).Name("Description");
                Map(m => m.Facilities).Name("Facilities");
                Map(m => m.Architect).Name("Architect");
                Map(m => m.CostMillionsEuros).Name("CostMillionsEuros");
                Map(m => m.Nickname).Name("Nickname");
            }
        }

        public class TeamMap : ClassMap<Team>
        {
            public TeamMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
                Map(m => m.ShortName).Name("ShortName");
                Map(m => m.FoundationDate).Name("FoundationDate");
                Map(m => m.PrimaryColor).Name("PrimaryColor");
                Map(m => m.SecondaryColor).Name("SecondaryColor");
                Map(m => m.Logo).Name("Logo");
                Map(m => m.City).Name("City");
                Map(m => m.Country).Name("Country");
                Map(m => m.League).Name("League");
                Map(m => m.StadiumId).Name("StadiumId");
                Map(m => m.Stadium).Ignore();
            }
        }

        public class CompetitionMap : ClassMap<Competition>
        {
            public CompetitionMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
                Map(m => m.Description).Name("Description");
                Map(m => m.Country).Name("Country");
                Map(m => m.Logo).Name("Logo");
            }
        }

        public class CoachMap : ClassMap<Coach>
        {
            public CoachMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.FirstName).Name("FirstName");
                Map(m => m.LastName).Name("LastName");
                Map(m => m.DateOfBirth).Name("DateOfBirth");
                Map(m => m.Nationality).Name("Nationality");
                Map(m => m.Role).Name("Role");
                Map(m => m.YearsOfExperience).Name("YearsOfExperience");
                Map(m => m.PhotoUrl).Name("PhotoUrl");
                Map(m => m.Biography).Name("Biography");
                Map(m => m.TeamId).Name("TeamId");
                Map(m => m.CoachingStyle).Name("CoachingStyle");
                Map(m => m.PreferredFormation).Name("PreferredFormation");
                Map(m => m.Team).Ignore();
            }
        }

        public class PlayerMap : ClassMap<Player>
        {
            public PlayerMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.FullName).Name("FullName");
                Map(m => m.KnownName).Name("KnownName");
                Map(m => m.Nationality).Name("Nationality");
                Map(m => m.ShirtNumber).Name("ShirtNumber");
                Map(m => m.PreferredFoot).Name("PreferredFoot");
                Map(m => m.TeamId).Name("TeamId");
                Map(m => m.PhotoUrl).Name("PhotoUrl");
                Map(m => m.CreatedAt).Name("CreatedAt");
                Map(m => m.UpdatedAt).Name("UpdatedAt");
                Map(m => m.Position).Name("Position");
                Map(m => m.Team).Ignore();
            }
        }

        public class SeasonMap : ClassMap<Season>
        {
            public SeasonMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
                Map(m => m.IsActive).Name("IsActive");
                Map(m => m.IsCompleted).Name("IsCompleted");
                Map(m => m.LeagueName).Name("LeagueName");
                Map(m => m.Country).Name("Country");
                Map(m => m.TotalRounds).Name("TotalRounds");
                Map(m => m.CurrentRound).Name("CurrentRound");
                Map(m => m.CreatedAt).Name("CreatedAt");
                Map(m => m.UpdatedAt).Name("UpdatedAt");
                Map(m => m.EndDate).Name("EndDate");
                Map(m => m.StartDate).Name("StartDate");
                Map(m => m.CompetitionId).Name("CompetitionId");
                Map(m => m.Competition).Ignore();
            }
        }

        public class TeamSeasonsMap : ClassMap<TeamSeasons>
        {
            public TeamSeasonsMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.TeamId).Name("TeamId");
                Map(m => m.SeasonId).Name("SeasonId");
                Map(m => m.UpdatedAt).Name("UpdatedAt");
                Map(m => m.Team).Ignore();
                Map(m => m.Season).Ignore();
            }
        }

        public class CustomBooleanConverter : CsvHelper.TypeConversion.DefaultTypeConverter
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
                {
                    return true;
                }
                if (
                    string.Equals(text, "f", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return false;
                }

                return base.ConvertFromString(text, row, memberMapData);
            }
        }

        public class UtcDateTimeConverter : CsvHelper.TypeConversion.DateTimeConverter
        {
            public override object? ConvertFromString(
                string? text,
                IReaderRow row,
                MemberMapData memberMapData
            )
            {
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                var dateTime = (DateTime?)base.ConvertFromString(text, row, memberMapData);
                return dateTime.HasValue
                    ? DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc)
                    : null;
            }
        }
    }
}