using System.Linq.Expressions;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Mappers;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Queries;

public class GetAllMatchesQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetAllMatchesQueryHandler _handler;
    private readonly Mock<MatchMapper> _matchMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllMatchesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _matchMapperMock = new Mock<MatchMapper>();
        _handler = new GetAllMatchesQueryHandler(_unitOfWorkMock.Object, _matchMapperMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
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
            TestDataBuilder.CreateValidMatch(3)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSeasonFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { HomeSeasonId = 1 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTeamFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { TeamId = 1 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "Completed" };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            MatchWeek = 1
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllMatchesQuery();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ThrowsAsync(new Exception(exceptionMessage));

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

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().BeEmpty();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAwaySeasonIdFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { AwaySeasonId = 2 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMatchWeekFilter_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery { MatchWeek = 5 };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyStringStatus_ShouldUseGetAllWithDetailsAsync()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "" };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
            TestDataBuilder.CreateValidMatch(3)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        // Should call GetAllWithDetailsAsync instead of filtered method since empty string is treated as no filter
        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Never);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_FilteredQueryException_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = "Completed" };
        var exceptionMessage = "Database query failed";

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);
        result.Matches.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MapperReturnsNull_ShouldHandleGracefully()
    {
        // Arrange
        var query = new GetAllMatchesQuery();
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns((List<MatchDto?>)null!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().BeNull();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_AllFiltersProvided_ReturnsFilteredMatches()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = 1,
            AwaySeasonId = 2,
            TeamId = 3,
            Status = "Live",
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow.AddDays(7),
            MatchWeek = 5
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            TestDataBuilder.CreateValidMatch(3)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(3);
        result.Error.Should().BeNull();

        // Should call GetAllWithDetailsAsync instead of filtered method since TeamId 0 is treated as no filter
        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Never);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Never);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialFilters_ShouldReturnCorrectResults()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = 1,
            Status = "Scheduled"
            // Other filters are null/default
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matches);
        _matchMapperMock.Setup(x => x.ToDtoList(matches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _matchMapperMock.Verify(x => x.ToDtoList(matches), Times.Once);
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
            ToDate = null
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1)
        };
        var matchesEnumerable = matches.AsEnumerable();
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matchesEnumerable);
        _matchMapperMock.Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullSeasonIds_ShouldTreatAsNoSeasonFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = null,
            AwaySeasonId = null,
            Status = "Live"
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1)
        };
        var matchesEnumerable = matches.AsEnumerable();
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matchesEnumerable);
        _matchMapperMock.Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullMatchWeek_ShouldTreatAsNoMatchWeekFilter()
    {
        // Arrange
        var query = new GetAllMatchesQuery
        {
            MatchWeek = null,
            Status = "Scheduled"
        };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1)
        };
        var matchesEnumerable = matches.AsEnumerable();
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(matchesEnumerable);
        _matchMapperMock.Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(1);

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidStatusValues_ShouldUseGetAllWithDetailsAsync(string? status)
    {
        // Arrange
        var query = new GetAllMatchesQuery { Status = status };
        var matches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };
        var matchesReadOnly = matches.AsReadOnly() as IReadOnlyList<Match>;
        var matchDtos = matches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllWithDetailsAsync())
            .ReturnsAsync(matchesReadOnly);
        _matchMapperMock.Setup(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);

        // Should use GetAllWithDetailsAsync for invalid status values
        _unitOfWorkMock.Verify(x => x.Matches.GetAllWithDetailsAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Never);
    }

    #endregion
}