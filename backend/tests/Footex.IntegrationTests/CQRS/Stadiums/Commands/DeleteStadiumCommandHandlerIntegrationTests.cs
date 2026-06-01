using Application.CQRS.Stadiums.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Commands;

public class DeleteStadiumCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidDeleteCommand_DeletesStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("DeleteTest");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var command = new DeleteStadiumCommand { Id = stadium.Id };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        var deletedStadium = await UnitOfWork.Stadiums.GetByIdAsync(stadium.Id);
        deletedStadium.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var command = new DeleteStadiumCommand { Id = 999999 };

        // Act
        var result = await Mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AlreadyDeletedId_ReturnsNotFound()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("DeleteTwiceTest");
        await UnitOfWork.Stadiums.AddAsync(stadium);
        await UnitOfWork.SaveChangesAsync();

        var command = new DeleteStadiumCommand { Id = stadium.Id };
        // First delete
        var firstResult = await Mediator.Send(command, CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue();

        // Act: Try to delete again
        var secondResult = await Mediator.Send(command, CancellationToken.None);

        // Assert
        secondResult.Succeeded.Should().BeFalse();
        secondResult.NotFound.Should().BeTrue();
        secondResult.Error.Should().Contain("not found");
    }
}
