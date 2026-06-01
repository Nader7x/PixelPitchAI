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

public class CreateTeamCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly CreateTeamCommandHandler _handler;
    private readonly Mock<ITeamMapper> _teamMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreateTeamCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _teamMapperMock = new Mock<ITeamMapper>();
        _handler = new CreateTeamCommandHandler(_unitOfWorkMock.Object, _teamMapperMock.Object);

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
        var command = _fixture
            .Build<CreateTeamCommand>()
            .With(c => c.Name, _fixture.Create<string>())
            .Without(c => c.CoachId)
            .Create();

        var teamToReturnFromMapper = new Team
        {
            Name = command.Name,
            ShortName = command.ShortName,
            Logo = command.Logo,
            Country = command.Country,
            City = command.City,
            League = command.League,
            FoundationDate = command.FoundationDate,
            PrimaryColor = command.PrimaryColor,
            SecondaryColor = command.SecondaryColor,
            StadiumId = command.StadiumId,
            Id = 0,
        };

        _teamMapperMock.Setup(m => m.ToTeamfromCreate(command)).Returns(teamToReturnFromMapper);

        _unitOfWorkMock.Setup(x => x.Teams.GetByNameAsync(command.Name)).ReturnsAsync((Team?)null);

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Coach?)null);

        _unitOfWorkMock
            .Setup(x => x.Teams.AddAsync(It.IsAny<Team>()))
            .Callback<Team>(team =>
            {
                team.Id = _fixture.Create<int>();
            });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.AddAsync(It.IsAny<Team>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(1)
        );
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<CreateTeamCommand>();

        _unitOfWorkMock
            .Setup(x => x.Teams.AddAsync(It.IsAny<Team>()))
            .Throws(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
