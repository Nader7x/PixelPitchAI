using Application.CQRS.Auth.Queries;
using Domain.Models;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Auth.Queries;

public class GetUserProfileQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    private readonly IMediator _mediator =
        factory.Services.GetRequiredService<IMediator>();

    private readonly UserManager<ApplicationUser> _userManager =
        factory.Services.GetRequiredService<UserManager<ApplicationUser>>();

    [Fact]
    public async Task Handle_ValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var password = "Password123!";
        var user = new ApplicationUser
        {

