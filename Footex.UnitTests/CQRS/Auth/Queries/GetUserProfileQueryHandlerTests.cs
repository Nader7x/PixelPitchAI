using Application.CQRS.Auth.Queries;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Auth.Queries;

public class GetUserProfileQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetUserProfileQueryHandler _handler;
    private readonly Mock<IApplicationUserRepository> _userRepositoryMock;

    public GetUserProfileQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IApplicationUserRepository>();
        _handler = new GetUserProfileQueryHandler(_userRepositoryMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidUser_ReturnsUserProfile()
    {
        // Arrange
        var user = _fixture.Create<ApplicationUser>();
        var query = new GetUserProfileQuery { UserId = user.Id };
        var roles = new List<string> { "User" };

        _userRepositoryMock.Setup(x => x.GetByIdAsyncWithTeam(user.Id)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetUserRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be(user.Id);
        result.Username.Should().Be(user.UserName);
        result.Email.Should().Be(user.Email);
        result.Roles.Should().BeEquivalentTo(roles);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidUser_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = "invalid_id" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsyncWithTeam("invalid_id"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_WithException_ReturnsErrorResponse()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = "any_id" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsyncWithTeam("any_id"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");
    }
}
