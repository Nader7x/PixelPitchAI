using Application.CQRS.Stadiums.Commands;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Commands;

public class DeleteStadiumCommandHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStadiumCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _mediator = factory.Services.GetRequiredService<IMediator>();
        _unitOfWork = factory.Services.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidDeleteCommand_DeletesStadiumSuccessfully()
    {
        // Arrange
        var stadium = TestData.CreateTestDbStadium("DeleteTest");
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var command = new DeleteStadiumCommand { Id = stadium.Id };

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        var deletedStadium = await _unitOfWork.Stadiums.GetByIdAsync(stadium.Id);
        deletedStadium.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var command = new DeleteStadiumCommand { Id = 999999 };

        // Act
        var result = await _mediator.Send(command, CancellationToken.None);

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
        await _unitOfWork.Stadiums.AddAsync(stadium);
        await _unitOfWork.SaveChangesAsync();

        var command = new DeleteStadiumCommand { Id = stadium.Id };
        // First delete
        var firstResult = await _mediator.Send(command, CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue();

        // Act: Try to delete again
        var secondResult = await _mediator.Send(command, CancellationToken.None);

        // Assert
        secondResult.Succeeded.Should().BeFalse();
        secondResult.NotFound.Should().BeTrue();
        secondResult.Error.Should().Contain("not found");
    }
}
