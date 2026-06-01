using System.Security.Claims;
using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class TeamsControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly TeamsController _controller;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITeamMapper> _teamMapperMock;
    private readonly TestFixtureBase _testFixtureBase;

    public TeamsControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _mediatorMock = new Mock<IMediator>();
        _teamMapperMock = new Mock<ITeamMapper>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new TeamsController(
            _mediatorMock.Object,
            _teamMapperMock.Object,
            _fileStorageServiceMock.Object,
            _cacheServiceMock.Object
        );

        // Setup controller context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Fact]
    public async Task GetAll_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllTeamsQueryResponse
        {
            Succeeded = true,
            Teams = new List<TeamDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Arsenal",
                    Country = "England",
                    City = "London",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllTeamsQueryResponse?)null);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllTeamsQuery>(), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllTeamsQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<ActionResult<GetAllTeamsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllTeamsQuery>(), default), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllTeamsQueryResponse
        {
            Succeeded = true,
            Teams = new List<TeamDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Arsenal",
                    Country = "England",
                    City = "London",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<ActionResult<GetAllTeamsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllTeamsQuery>(), default), Times.Never);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var teamId = 1;
        var expectedResponse = new GetTeamByIdQueryResponse
        {
            Succeeded = true,
            Team = new TeamDto
            {
                Id = teamId,
                Name = "Arsenal",
                Country = "England",
                City = "London",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetTeamByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetTeamByIdQueryResponse?)null);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetTeamByIdQuery>(q => q.Id == teamId), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetTeamByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetById(teamId);

        // Assert
        result.Should().BeOfType<ActionResult<GetTeamByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<GetTeamByIdQuery>(q => q.Id == teamId), default),
            Times.Once
        );
    }

    [Fact]
    public async Task Create_WithValidTeam_ReturnsCreatedResult()
    {
        // Arrange
        var teamDto = new CreateTeamDto
        {
            Name = "Arsenal",
            Country = "England",
            League = "Premier League",
            FoundationDate = DateTime.Now.AddYears(-100),
        };

        var createCommand = new CreateTeamCommand
        {
            Name = teamDto.Name,
            Country = teamDto.Country,
            League = teamDto.League,
            FoundationDate = teamDto.FoundationDate,
        };

        var expectedResponse = new CreateTeamCommandResponse
        {
            Succeeded = true,
            Id = 1,
            Name = teamDto.Name,
        };

        _teamMapperMock
            .Setup(x => x.ToCreateCommand(It.IsAny<CreateTeamDto>()))
            .Returns(createCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateTeamCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Create(teamDto);

        // Assert
        result.Should().BeOfType<ActionResult<CreateTeamCommandResponse>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<CreateTeamCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_WithValidTeam_ReturnsOkResult()
    {
        // Arrange
        var teamId = 1;
        var teamDto = new UpdateTeamDto
        {
            Name = "Arsenal Updated",
            Country = "England",
            City = "London",
        };

        var updateCommand = new UpdateTeamCommand
        {
            Id = teamId,
            Name = teamDto.Name,
            Country = teamDto.Country,
            City = teamDto.City,
        };

        var expectedResponse = new UpdateTeamCommandResponse
        {
            Succeeded = true,
            Id = teamId,
            Name = teamDto.Name,
        };

        _teamMapperMock
            .Setup(x => x.ToUpdateCommand(It.IsAny<UpdateTeamDto>()))
            .Returns(updateCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateTeamCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Update(teamId, teamDto);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateTeamCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateTeamCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var teamId = 1;
        var expectedResponse = new DeleteTeamCommandResponse { Succeeded = true };

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeleteTeamCommand>(c => c.Id == teamId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Delete(teamId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull();

        _mediatorMock.Verify(
            x => x.Send(It.Is<DeleteTeamCommand>(c => c.Id == teamId), default),
            Times.Once
        );
    }
}
