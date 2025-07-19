using Application.CQRS.Teams.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Teams.Commands;

public class UpdateTeamCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly UpdateTeamCommandHandler _handler;
    private readonly Mock<ITeamMapper> _teamMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdateTeamCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _teamMapperMock = new Mock<ITeamMapper>();
        _handler = new UpdateTeamCommandHandler(_unitOfWorkMock.Object, _teamMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new UpdateTeamCommand { Id = 1, Name = "Real Madrid" };

        var existingTeam = new Team { Id = command.Id, Name = "Real Madrid" };

        _unitOfWorkMock
            .Setup(x => x.Teams.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTeam);

        _teamMapperMock
            .Setup(x => x.UpdateTeamFromCommand(It.IsAny<UpdateTeamCommand>(), It.IsAny<Team>()))
            .Callback<UpdateTeamCommand, Team>(
                (teamCommand, team) =>
                {
                    team.Id = teamCommand.Id;
                    if (teamCommand.Name != null)
                        team.Name = teamCommand.Name;
                    if (teamCommand.ShortName != null)
                        team.ShortName = teamCommand.ShortName;
                    if (teamCommand.Logo != null)
                        team.Logo = teamCommand.Logo;
                    if (teamCommand.Country != null)
                        team.Country = teamCommand.Country;
                    if (teamCommand.City != null)
                        team.City = teamCommand.City;
                    if (teamCommand.League != null)
                        team.League = teamCommand.League;
                    if (teamCommand.StadiumId != null)
                        team.StadiumId = teamCommand.StadiumId.Value;
                    if (teamCommand.FoundationDate != null)
                        team.FoundationDate = teamCommand.FoundationDate.Value;
                    if (teamCommand.PrimaryColor != null)
                        team.PrimaryColor = teamCommand.PrimaryColor;
                    if (teamCommand.SecondaryColor != null)
                        team.SecondaryColor = teamCommand.SecondaryColor;
                }
            );
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Id.Should().Be(command.Id);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentTeam_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new UpdateTeamCommand { Id = 999, Name = "Real Madrid" };

        _unitOfWorkMock
            .Setup(x => x.Teams.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Team with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdateTeamCommand { Id = 1, Name = "Real Madrid" };

        _unitOfWorkMock
            .Setup(x => x.Teams.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
