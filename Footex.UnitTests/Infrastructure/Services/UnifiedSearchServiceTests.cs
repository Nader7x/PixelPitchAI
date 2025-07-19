using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services;

// Helper classes for mocking EF Core async operations
public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable) { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })
            ?.MakeGenericMethod(resultType)
            .Invoke(this, new[] { expression });

        return (TResult)
            (
                typeof(Task)
                    .GetMethod(nameof(Task.FromResult))
                    ?.MakeGenericMethod(resultType)
                    .Invoke(null, new[] { executionResult }) ?? Task.FromResult(default(TResult))
            )!;
    }
}

public class UnifiedSearchServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<UnifiedSearchService>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;

    private readonly List<Coach> _testCoaches = new()
    {
        new Coach { Id = 1, FirstName = "Coach A" },
        new Coach { Id = 2, FirstName = "Coach B" },
    };

    private readonly List<Match> _testMatches = new()
    {
        new Match
        {
            Id = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MatchStatus = "Scheduled",
            CreatorId = "Creator1",
        },
        new Match
        {
            Id = 2,
            HomeTeamId = 3,
            AwayTeamId = 1,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(2),
            MatchStatus = "Scheduled",
            CreatorId = "Creator2",
        },
    };

    // Re-usable test data
    private readonly List<Player> _testPlayers = new()
    {
        new Player
        {
            Id = 1,
            FullName = "Jn Doe",
            Position = "Forward",
        },
        new Player
        {
            Id = 2,
            FullName = "Jane Smith",
            Position = "Midfielder",
        },
        new Player
        {
            Id = 3,
            FullName = "Jn Test",
            Position = "Defender",
        },
    };

    private readonly List<Stadium> _testStadiums = new()
    {
        new Stadium
        {
            Id = 1,
            Name = "Wembley Stadium",
            City = "London",
            Capacity = 90000,
        },
        new Stadium
        {
            Id = 2,
            Name = "Old Trafford",
            City = "Manchester",
            Capacity = 74879,
        },
        new Stadium
        {
            Id = 3,
            Name = "Anfield",
            City = "Liverpool",
            Capacity = 54074,
        },
    };

    private readonly List<Team> _testTeams = new()
    {
        new Team
        {
            Id = 1,
            Name = "Team Alpha",
            City = "City A",
            League = "League X",
            Country = "Country 1",
        },
        new Team
        {
            Id = 2,
            Name = "Team Beta",
            City = "City B",
            League = "League Y",
            Country = "Country 2",
        },
        new Team
        {
            Id = 3,
            Name = "Team Gamma",
            City = "City C",
            League = "League X",
            Country = "Country 1",
        },
    };

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ICoachRepository> _mockCoachRepo = null!;
    private Mock<IMatchRepository> _mockMatchRepo = null!;
    private Mock<IPlayerRepository> _mockPlayerRepo = null!;
    private Mock<IStadiumsRepository> _mockStadiumRepo = null!;
    private Mock<ITeamRepository> _mockTeamRepo = null!;
    private UnifiedSearchService _searchService = null!;

    public UnifiedSearchServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UnifiedSearchService>>();
        _configurationMock = new Mock<IConfiguration>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        // Initial configuration setup, can be overridden by SetupMocks
        var mockConfSectionEnableFts = new Mock<IConfigurationSection>();
        mockConfSectionEnableFts.Setup(s => s.Value).Returns("false");
        _configurationMock
            .Setup(c => c.GetSection("Search:EnablePostgreSQLFullText"))
            .Returns(mockConfSectionEnableFts.Object);

        var mockConfSectionFuzzyThreshold = new Mock<IConfigurationSection>();
        mockConfSectionFuzzyThreshold.Setup(s => s.Value).Returns("3");
        _configurationMock
            .Setup(c => c.GetSection("Search:FuzzySearchThreshold"))
            .Returns(mockConfSectionFuzzyThreshold.Object);
    }

    private void SetupMocks(
        List<Player> players,
        List<Team> teams,
        List<Coach> coaches,
        List<Match> matches,
        List<Stadium> stadiums,
        bool enableFts = false,
        string fuzzyThreshold = "3"
    )
    {
        // Re-setup configuration based on parameters for each test
        var mockConfSectionEnableFts = new Mock<IConfigurationSection>();
        mockConfSectionEnableFts.Setup(s => s.Value).Returns(enableFts.ToString().ToLower());
        _configurationMock
            .Setup(c => c.GetSection("Search:EnablePostgreSQLFullText"))
            .Returns(mockConfSectionEnableFts.Object);

        var mockConfSectionFuzzyThreshold = new Mock<IConfigurationSection>();
        mockConfSectionFuzzyThreshold.Setup(s => s.Value).Returns(fuzzyThreshold);
        _configurationMock
            .Setup(c => c.GetSection("Search:FuzzySearchThreshold"))
            .Returns(mockConfSectionFuzzyThreshold.Object);

        // Mock DbContext and DbSets for all search operations
        var dbContextOptions = new DbContextOptions<FootballDbContext>();
        var mockDbContext = new Mock<FootballDbContext>(dbContextOptions);

        mockDbContext.Setup(c => c.Players).Returns(CreateMockDbSet(players).Object);
        mockDbContext.Setup(c => c.Teams).Returns(CreateMockDbSet(teams).Object);
        mockDbContext.Setup(c => c.Coaches).Returns(CreateMockDbSet(coaches).Object);
        mockDbContext.Setup(c => c.Matches).Returns(CreateMockDbSet(matches).Object);
        mockDbContext.Setup(c => c.Stadiums).Returns(CreateMockDbSet(stadiums).Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(FootballDbContext)))
            .Returns(mockDbContext.Object);
        // If IUnitOfWork is also resolved from the scope for search operations, mock it here too.
        // Otherwise, it remains as a direct injection in the UnifiedSearchService constructor.
        // Assuming for now, FootballDbContext is primary search source.
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IUnitOfWork)))
            .Returns(_unitOfWorkMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);

        _mockPlayerRepo = new Mock<IPlayerRepository>();
        _mockTeamRepo = new Mock<ITeamRepository>();
        _mockCoachRepo = new Mock<ICoachRepository>();
        _mockMatchRepo = new Mock<IMatchRepository>();
        _mockStadiumRepo = new Mock<IStadiumsRepository>();

        // Setup SearchAsync for each repository to return the test data
        _mockPlayerRepo.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync(players);
        _mockTeamRepo.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync(teams);
        _mockCoachRepo.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync(coaches);
        _mockMatchRepo.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync(matches);
        _mockStadiumRepo.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync(stadiums);

        // Setup IUnitOfWork to return these mocked repository objects
        _unitOfWorkMock.Setup(uow => uow.Players).Returns(_mockPlayerRepo.Object);
        _unitOfWorkMock.Setup(uow => uow.Teams).Returns(_mockTeamRepo.Object);
        _unitOfWorkMock.Setup(uow => uow.Coaches).Returns(_mockCoachRepo.Object);
        _unitOfWorkMock.Setup(uow => uow.Matches).Returns(_mockMatchRepo.Object);
        _unitOfWorkMock.Setup(uow => uow.Stadiums).Returns(_mockStadiumRepo.Object);

        // Initialize _searchService here to ensure it uses the mocks set up for the current test
        _searchService = new UnifiedSearchService(
            _serviceScopeFactoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _configurationMock.Object
        );
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList)
        where T : class
    {
        var queryable = sourceList.AsQueryable();
        var dbSet = new Mock<DbSet<T>>();

        dbSet
            .As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
        dbSet
            .As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return dbSet;
    }

    [Fact]
    public async Task SearchAsync_WithShortQuery_ReturnsEmptyResult()
    {
        // Arrange
        SetupMocks(
            new List<Player>(),
            new List<Team>(),
            new List<Coach>(),
            new List<Match>(),
            new List<Stadium>()
        );
        var query = "a";

        // Act
        var result = await _searchService.SearchAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalResults);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsCorrectResults()
    {
        // Arrange
        var query = "Test";
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        // Act
        var result = await _searchService.SearchAsync(query);

        // Assert
        Assert.NotNull(result);
        // Based on _testPlayers, only "Johnny Test" contains "Test"
        Assert.Equal(1, result.TotalResults);
        Assert.NotEmpty(result.Items);

        var playerResult = result.Items.FirstOrDefault(i => i.Type == "Player");
        Assert.NotNull(playerResult);
        Assert.Equal("Jn Test", playerResult.Name);

        // No team in _testTeams contains "Test"
        var teamResult = result.Items.FirstOrDefault(i => i.Type == "Team");
        Assert.Null(teamResult);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatchingResults_ReturnsEmptyResult()
    {
        // Arrange
        var query = "NonExistent";
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        // Act
        var result = await _searchService.SearchAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalResults);
    }

    [Fact]
    public async Task SearchAsync_WhenExceptionOccurs_ReturnsError()
    {
        // Arrange
        var query = "error";
        var dbContextOptions = new DbContextOptions<FootballDbContext>();
        var mockDbContext = new Mock<FootballDbContext>(dbContextOptions);
        mockDbContext.Setup(c => c.Teams).Throws(new Exception("Simulated Database Exception"));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(FootballDbContext)))
            .Returns(mockDbContext.Object);
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IUnitOfWork)))
            .Returns(_unitOfWorkMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);

        // Re-initialize _searchService with the throwing DbContext mock for this specific test
        _searchService = new UnifiedSearchService(
            _serviceScopeFactoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _configurationMock.Object
        );

        // Act
        var result = await _searchService.SearchAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Error);
        Assert.Contains("Simulated Database Exception", result.Error);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchWithStrategyAsync_WithFuzzyStrategy_ReturnsFuzzyResults()
    {
        // Arrange
        var query = "test";
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        // Act
        var result = await _searchService.SearchWithStrategyAsync(query, SearchStrategy.Fuzzy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalResults);
        Assert.Contains(result.Items, item => item.Name == "Jn Test");
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithNullFilters_ReturnsEmptyResult()
    {
        // Arrange
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        // Act
        var result = await _searchService.SearchWithFiltersAsync(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalResults);
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithTeamEntityTypeAndQuery_ReturnsFilteredTeams()
    {
        // Arrange
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        var filters = new SearchFiltersDto
        {
            Query = "Alpha",
            EntityTypes = new List<string> { "Team" },
        };

        _unitOfWorkMock
            .Setup(uow => uow.Teams.GetQueryable())
            .Returns(_testTeams.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalResults);
        var teamResult = result.Items.First();
        Assert.Equal("Team", teamResult.Type);
        Assert.Equal("Team Alpha", teamResult.Name);
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithCountryAndLeagueFilters_ReturnsFilteredTeams()
    {
        // Arrange
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        var filters = new SearchFiltersDto
        {
            EntityTypes = new List<string> { "Team" },
            Country = "Country 1",
            League = "League X",
        };
        _unitOfWorkMock
            .Setup(uow => uow.Teams.GetQueryable())
            .Returns(_testTeams.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalResults);
        Assert.Contains(result.Items, item => item.Name == "Team Alpha");
        Assert.Contains(result.Items, item => item.Name == "Team Gamma");
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithQueryAndEntityType_ReturnsMatchingPlayers()
    {
        // Arrange
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        var filters = new SearchFiltersDto
        {
            Query = "Jn",
            EntityTypes = new List<string> { "Player" },
        };
        // Update TeamId properties on the existing _testPlayers
        _testPlayers[0].TeamId = 1;
        _testPlayers[1].TeamId = 2;
        _testPlayers[2].TeamId = 3;

        // Link the navigation properties
        foreach (var player in _testPlayers)
            player.Team = _testTeams.FirstOrDefault(t => t.Id == player.TeamId);

        _unitOfWorkMock
            .Setup(uow =>
                uow.Players.GetQueryable(
                    It.IsAny<Expression<Func<Player, bool>>?>(),
                    It.IsAny<Expression<Func<Player, object>>[]>()
                )
            )
            .Returns(_testPlayers.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalResults);
        Assert.Contains(result.Items, p => p.Name == "Jn Doe");
        Assert.Contains(result.Items, p => p.Name == "Jn Test");
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithPlayerPositionFilter_ReturnsCorrectResults()
    {
        // Arrange
        var filters = new SearchFiltersDto
        {
            Position = "Forward",
            EntityTypes = new List<string> { "Player" },
        };
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        _testPlayers[0].TeamId = 1;
        _testPlayers[1].TeamId = 2;
        _testPlayers[2].TeamId = 3;

        // Link the navigation properties
        foreach (var player in _testPlayers)
            player.Team = _testTeams.FirstOrDefault(t => t.Id == player.TeamId);

        _unitOfWorkMock
            .Setup(uow =>
                uow.Players.GetQueryable(
                    It.IsAny<Expression<Func<Player, bool>>?>(),
                    It.IsAny<Expression<Func<Player, object>>[]>()
                )
            )
            .Returns(_testPlayers.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, item => item.Name == "Jn Doe");
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithTeamFilter_ReturnsCorrectResults()
    {
        // Arrange
        var filters = new SearchFiltersDto
        {
            Query = "Team Alpha",
            EntityTypes = new List<string> { "Team" },
        };
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);

        _unitOfWorkMock
            .Setup(uow => uow.Teams.GetQueryable())
            .Returns(_testTeams.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, item => item.Name == "Team Alpha");
    }

    [Fact]
    public async Task SearchWithFiltersAsync_WithMultipleFilters_ReturnsCorrectResults()
    {
        // Arrange
        var filters = new SearchFiltersDto
        {
            Position = "Midfielder",
            Query = "Team Beta",
            EntityTypes = new List<string> { "Team", "Player" },
        };
        SetupMocks(_testPlayers, _testTeams, _testCoaches, _testMatches, _testStadiums);
        _testPlayers[0].TeamId = 1;
        _testPlayers[1].TeamId = 2;
        _testPlayers[2].TeamId = 3;

        // Link the navigation properties
        foreach (var player in _testPlayers)
            player.Team = _testTeams.FirstOrDefault(t => t.Id == player.TeamId);

        _unitOfWorkMock
            .Setup(uow =>
                uow.Players.GetQueryable(
                    It.IsAny<Expression<Func<Player, bool>>?>(),
                    It.IsAny<Expression<Func<Player, object>>[]>()
                )
            )
            .Returns(_testPlayers.AsQueryable().BuildMock());
        _unitOfWorkMock
            .Setup(uow => uow.Teams.GetQueryable())
            .Returns(_testTeams.AsQueryable().BuildMock());

        // Act
        var result = await _searchService.SearchWithFiltersAsync(filters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalResults);
        Assert.Contains(result.Items, item => item.Name == "Jane Smith");
        Assert.Contains(result.Items, item => item.Name == "Team Beta");
    }
}
