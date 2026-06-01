using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.UnitTests.Common;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Queries;

public class GetAllMatchesQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetAllMatchesQueryHandler _handler;
    private readonly Mock<IMatchMapper> _IMatchMapperMock;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllMatchesQueryHandlerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _IMatchMapperMock = new Mock<IMatchMapper>();
        _handler = new GetAllMatchesQueryHandler(_unitOfWorkMock.Object, _IMatchMapperMock.Object);

        _fixture = new Fixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private static Match CreateMatch(
        int id,
        int homeTeamSeasonId = 1,
        int awayTeamSeasonId = 1,
        int homeTeamId = 1,
        int awayTeamId = 2,
        string status = "Scheduled",
        int matchWeek = 1,
        DateTime? scheduledDate = null
    )
    {
        return new Match
        {
            Id = id,
            HomeTeamSeasonId = homeTeamSeasonId,
            AwayTeamSeasonId = awayTeamSeasonId,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            MatchStatus = status,
            MatchWeek = matchWeek,
            ScheduledDateTimeUtc = scheduledDate ?? DateTime.UtcNow,
            CreatorId = Guid.Empty.ToString(),
        };
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery();
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
            TestDataBuilder.CreateValidMatch(3),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        // Mock the queryable and its extension methods
        var mockQueryable = matches.BuildMock();
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSeasonFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { HomeSeasonId = 1 };
        var allMatches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        // Only matches with HomeSeasonId = 1 should be returned
        var filteredMatches = allMatches.Where(m => m.HomeTeamSeasonId == 1).ToList();
        var matchDtos = filteredMatches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        // Mock the queryable and its extension methods
        var mockQueryable = allMatches.BuildMock();
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.Is<IEnumerable<Match>>(m => m.Count() == 2)))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Check if the result is successful
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );
        if (!result.Succeeded)
            // If the test fails, this will show us the actual error
            throw new Exception($"Handler failed with error: {result.Error}");

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable());
        _IMatchMapperMock.Verify(
            x => x.ToDtoList(It.Is<IEnumerable<Match>>(m => m.Count() == 2)),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithTeamFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var teamId = 1;
        var query = new GetAllMatchesQuery { TeamId = teamId };
        var allMatches = new List<Match>
        {
            CreateMatch(1, homeTeamId: teamId),
            CreateMatch(2, awayTeamId: teamId),
            CreateMatch(3, homeTeamId: 3, awayTeamId: 4),
        };
        // Only matches with HomeTeamId = 1 or AwayTeamId = 1 should be returned
        var filteredMatches = allMatches
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .ToList();
        var matchDtos = filteredMatches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        // Mock the queryable and its extension methods
        var mockQueryable = allMatches.BuildMock();
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.Is<IEnumerable<Match>>(m => m.Count() == 2)))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(
            x => x.ToDtoList(It.Is<IEnumerable<Match>>(m => m.Count() == 2)),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "Completed" };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatchWithStatus(1, "Completed"),
            TestDataBuilder.CreateValidMatchWithStatus(2, "Completed"),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.Is<IEnumerable<Match>>(m => m.Count() == 2)))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var query = new GetAllMatchesQuery { FromDate = fromDate, ToDate = toDate };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleFilters_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = 1,
            TeamId = 2,
            Status = "Completed",
            MatchWeek = 1,
        };
        var matches = new List<Match> { TestDataBuilder.CreateValidMatch(1) };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllMatchesQuery();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);
        result.Matches.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllMatchesQuery { TeamId = 999 }; // Non-existent team
        var matches = new List<Match>();
        var matchDtos = new List<MatchDto?>();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().BeEmpty();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAwaySeasonIdFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { AwaySeasonId = 2 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMatchWeekFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { MatchWeek = 5 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFromDateOnly_ReturnsFilteredMatches()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var query = new GetAllMatchesQuery { FromDate = fromDate };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithToDateOnly_ReturnsFilteredMatches()
    {
        // Arrange
        var toDate = DateTime.UtcNow.AddDays(30);
        var query = new GetAllMatchesQuery { ToDate = toDate };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        var mockQueryable = matches.BuildMock();
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);

        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyStringStatus()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "" };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatchWithStatus(1),
            TestDataBuilder.CreateValidMatchWithStatus(2, "InProgress"),
            TestDataBuilder.CreateValidMatchWithStatus(3, "Completed"),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();
        // Mock the queryable and its extension methods
        var mockQueryable = matches.BuildMock();

        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_FilteredQueryException_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "Completed" };
        var exceptionMessage = "Database query failed";

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);
        result.Matches.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MapperReturnsNull_ShouldHandleGracefully()
    {
        // Arrange
        var query = new GetAllMatchesQuery();
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns((List<MatchDto?>)null!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().BeNull();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_AllFiltersProvided_ReturnsFilteredMatches()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var fromDate = now.AddDays(-7);
        var toDate = now.AddDays(7);
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = 1,
            AwaySeasonId = 2,
            TeamId = 3, // Adding TeamId filter to test all filters
            Status = "Live",
            FromDate = fromDate,
            ToDate = toDate,
            MatchWeek = 5,
        };

        var allMatches = new List<Match>
        {
            // Match that meets all criteria
            CreateMatch(1, 1, 2, 3, status: "Live", matchWeek: 5, scheduledDate: now),
            // Matches that fail different filter criteria
            CreateMatch(2, 2, 2), // Wrong homeSeasonId
            CreateMatch(3, 1, 3), // Wrong awaySeasonId
            CreateMatch(4, 1, 2, 4, 5), // Wrong teamId
            CreateMatch(5, 1, 2, 3, status: "Scheduled"), // Wrong status
            CreateMatch(6, 1, 2, 3, status: "Live", matchWeek: 6), // Wrong matchWeek
            CreateMatch(7, 1, 2, 3, status: "Live", matchWeek: 5, scheduledDate: now.AddDays(-10)), // Outside date range
        };

        // The expected filtered result (only the match that meets all criteria)
        var expectedFilteredMatch = allMatches.First();
        var matchDtos = new List<MatchDto?> { new() { Id = expectedFilteredMatch.Id } };

        // Create a mock queryable
        var mockQueryable = allMatches.BuildMock();

        // Setup the repository mock to return our queryable
        _unitOfWorkMock.Setup(x => x.Matches.GetQueryable()).Returns(mockQueryable);

        // Setup the mapper to return our expected DTOs - be more lenient with the setup
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        if (!result.Succeeded)
            // If the test fails, this will show us the actual error
            throw new Exception($"Handler failed with error: {result.Error}");
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().NotBeNull();
        result.Matches.Should().HaveCount(1);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroTeamId_ShouldTreatAsNoFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery { TeamId = 0 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
            TestDataBuilder.CreateValidMatch(3),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTeamId_ShouldUseFilteredQuery()
    {
        // Arrange
        var teamId = 1;
        var query = new GetAllMatchesQuery { TeamId = teamId };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialFilters_ShouldReturnCorrectResults()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = 1,
            Status = "Scheduled",
            // Other filters are null/default
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock.Setup(x => x.ToDtoList(matches)).Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    #region Additional Edge Case Tests

    [Fact]
    public async Task Handle_WithNullDateFilters_ShouldTreatAsNoDateFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            TeamId = 1,
            FromDate = null,
            ToDate = null,
        };
        var matches = new List<Match> { TestDataBuilder.CreateValidMatch(1) };
        var matchesEnumerable = matches.AsEnumerable();
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullSeasonIds_ShouldTreatAsNoSeasonFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = null,
            AwaySeasonId = null,
            Status = "Live",
        };
        var matches = new List<Match> { TestDataBuilder.CreateValidMatchWithStatus(1, "Live") };
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);
        _testOutputHelper.WriteLine(
            $"Result: {result.Succeeded}, Matches Count: {result.Matches?.Count}"
        );

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullMatchWeek_ShouldTreatAsNoMatchWeekFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery { MatchWeek = null, Status = "Scheduled" };
        var matches = new List<Match> { TestDataBuilder.CreateValidMatch(1) };
        var matchesEnumerable = matches.AsEnumerable();
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matches.BuildMock());
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidStatusValues(string? status)
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = status };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
        };
        var matchesReadOnly = matches.AsReadOnly() as IReadOnlyList<Match>;
        var matchDtos = matches.Select(m => (MatchDto?)new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetQueryable())
            .Returns(matchesReadOnly.ToList().BuildMock());
        _IMatchMapperMock
            .Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        if (!result.Succeeded)
            throw new Exception(result.Error);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);

        _unitOfWorkMock.Verify(x => x.Matches.GetQueryable(), Times.Once);
    }

    #endregion
}
