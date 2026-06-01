using Application.DTOs;
using Application.Interfaces;
using AutoFixture;
using Domain.Models;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class SearchControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly Mock<IAdvancedSearchService> _advancedSearchServiceMock;
    private readonly SearchController _controller;
    private readonly NoRecursionFixture _fixture;
    private readonly TestFixtureBase _testFixtureBase;

    public SearchControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _advancedSearchServiceMock = new Mock<IAdvancedSearchService>();

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new SearchController(_advancedSearchServiceMock.Object);
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Arsenal";
        var page = 1;
        var pageSize = 10;

        var expectedResponse = new SearchResultDto
        {
            TotalResults = 5,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = 1,
            Items = new List<SearchItemDto>
            {
                new()
                {
                    Id = "1",
                    Name = "Arsenal FC",
                    Type = "Team",
                    Description = "Arsenal Football Club",
                },
            },
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchAsync(query, page, pageSize))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, page, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _advancedSearchServiceMock.Verify(x => x.SearchAsync(query, page, pageSize), Times.Once);
    }

    [Fact]
    public async Task Search_WithShortQuery_ReturnsBadRequest()
    {
        // Arrange
        var shortQuery = "A";

        // Act
        var result = await _controller.Search(shortQuery);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Search query must be at least 2 characters long");

        _advancedSearchServiceMock.Verify(
            x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var emptyQuery = "";

        // Act
        var result = await _controller.Search(emptyQuery);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Search query must be at least 2 characters long");

        _advancedSearchServiceMock.Verify(
            x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Search_WithInvalidPage_UsesDefaultPage()
    {
        // Arrange
        var query = "Arsenal";
        var invalidPage = 0;
        var pageSize = 10;

        var expectedResponse = new SearchResultDto
        {
            TotalResults = 5,
            CurrentPage = 1, // Should default to 1
            PageSize = pageSize,
            TotalPages = 1,
            Items = new List<SearchItemDto>(),
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchAsync(query, 1, pageSize)) // Should call with page = 1
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, invalidPage, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _advancedSearchServiceMock.Verify(x => x.SearchAsync(query, 1, pageSize), Times.Once);
    }

    [Fact]
    public async Task Search_WithInvalidPageSize_UsesDefaultPageSize()
    {
        // Arrange
        var query = "Arsenal";
        var page = 1;
        var invalidPageSize = 100; // > 50

        var expectedResponse = new SearchResultDto
        {
            TotalResults = 5,
            CurrentPage = page,
            PageSize = 10, // Should default to 10
            TotalPages = 1,
            Items = new List<SearchItemDto>(),
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchAsync(query, page, 10)) // Should call with pageSize = 10
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, page, invalidPageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _advancedSearchServiceMock.Verify(x => x.SearchAsync(query, page, 10), Times.Once);
    }

    [Fact]
    public async Task SearchWithStrategy_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Arsenal";
        var strategy = SearchStrategy.Fuzzy;
        var page = 1;
        var pageSize = 10;

        var expectedResponse = new SearchResultDto { TotalResults = 1 };

        _advancedSearchServiceMock
            .Setup(x => x.SearchWithStrategyAsync(query, strategy, page, pageSize))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchWithStrategy(query, strategy, page, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _advancedSearchServiceMock.Verify(
            x => x.SearchWithStrategyAsync(query, strategy, page, pageSize),
            Times.Once
        );
    }

    [Fact]
    public async Task SearchWithFilters_WithValidFilters_ReturnsOkResult()
    {
        // Arrange
        var filters = new SearchFiltersDto
        {
            Query = "Arsenal",
            Page = 1,
            PageSize = 10,
        };

        var expectedResponse = new SearchResultDto { TotalResults = 1 };

        _advancedSearchServiceMock
            .Setup(x => x.SearchWithFiltersAsync(filters))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchWithFilters(filters);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _advancedSearchServiceMock.Verify(x => x.SearchWithFiltersAsync(filters), Times.Once);
    }

    [Fact]
    public async Task UnifiedSearch_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "test";
        var entityTypes = "Team,Player";
        var page = 1;
        var pageSize = 10;
        var expectedResponse = new SearchResultDto { TotalResults = 2 };

        _advancedSearchServiceMock
            .Setup(x => x.UnifiedSearchAsync(query, It.IsAny<List<string>>(), page, pageSize))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UnifiedSearch(query, entityTypes, page, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SearchAll_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "test";
        var page = 1;
        var pageSize = 10;
        var enableFuzzySearch = true;
        var expectedResponse = new SearchResultDto { TotalResults = 5 };

        _advancedSearchServiceMock
            .Setup(x => x.SearchAllAsync(query, page, pageSize, enableFuzzySearch))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchAll(query, page, pageSize, enableFuzzySearch);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SearchTeams_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Arsenal";
        var limit = 10;
        var expectedResponse = new List<Team>
        {
            new() { Id = 1, Name = "Arsenal" },
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchTeamsAsync(query, limit, false))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchTeams(query, limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SearchPlayers_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Saka";
        var limit = 10;
        var expectedResponse = new List<Player>
        {
            new() { Id = 1, FullName = "Bukayo Saka" },
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchPlayersAsync(query, limit, false))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchPlayers(query, limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SearchCoaches_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Arteta";
        var limit = 10;
        var expectedResponse = new List<Coach>
        {
            new()
            {
                Id = 1,
                FirstName = "Mikel",
                LastName = "Arteta",
            },
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchCoachesAsync(query, limit, false))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchCoaches(query, limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetSearchSuggestions_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "Ars";
        var limit = 5;
        var expectedResponse = new List<SearchSuggestionDto> { new() { Text = "Arsenal" } };

        _advancedSearchServiceMock
            .Setup(x => x.GetSearchSuggestionsAsync(query, limit))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSearchSuggestions(query, limit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task BulkSearch_WithValidQueries_ReturnsOkResult()
    {
        // Arrange
        var queries = new List<string> { "Arsenal", "Saka" };
        var pageSize = 10;
        var expectedResponse = new Dictionary<string, SearchResultDto>
        {
            {
                "Arsenal",
                new SearchResultDto { TotalResults = 1 }
            },
            {
                "Saka",
                new SearchResultDto { TotalResults = 1 }
            },
        };

        _advancedSearchServiceMock
            .Setup(x => x.SearchAsync("Arsenal", 1, pageSize))
            .ReturnsAsync(new SearchResultDto { TotalResults = 1 });
        _advancedSearchServiceMock
            .Setup(x => x.SearchAsync("Saka", 1, pageSize))
            .ReturnsAsync(new SearchResultDto { TotalResults = 1 });

        // Act
        var result = await _controller.BulkSearch(queries, pageSize);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
