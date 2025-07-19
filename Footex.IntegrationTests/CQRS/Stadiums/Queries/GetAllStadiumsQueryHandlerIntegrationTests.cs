using Application.CQRS.Stadiums.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Stadiums.Queries;

public class GetAllStadiumsQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public GetAllStadiumsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _mediator = factory.Services.GetRequiredService<IMediator>();
        _unitOfWork = factory.Services.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllStadiums()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("StadiumA");
        var stadium2 = TestData.CreateTestDbStadium("StadiumB");
        await _unitOfWork.Stadiums.AddAsync(stadium1);
        await _unitOfWork.Stadiums.AddAsync(stadium2);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetAllStadiumsQuery();

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums.Should().Contain(s => s.Name == stadium1.Name);
        result.Stadiums.Should().Contain(s => s.Name == stadium2.Name);
    }

    [Fact]
    public async Task Handle_FilterByCountry_ReturnsFilteredStadiums()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("EnglandStadium");
        stadium1.Country = "England";
        var stadium2 = TestData.CreateTestDbStadium("SpainStadium");
        stadium2.Country = "Spain";
        await _unitOfWork.Stadiums.AddAsync(stadium1);
        await _unitOfWork.Stadiums.AddAsync(stadium2);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetAllStadiumsQuery { Country = "England" };

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().OnlyContain(s => s.Country == "England");
    }

    [Fact]
    public async Task Handle_FilterByCity_ReturnsFilteredStadiums()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("LondonStadium");
        stadium1.City = "London";
        var stadium2 = TestData.CreateTestDbStadium("MadridStadium");
        stadium2.City = "Madrid";
        await _unitOfWork.Stadiums.AddAsync(stadium1);
        await _unitOfWork.Stadiums.AddAsync(stadium2);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetAllStadiumsQuery { City = "London" };

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().OnlyContain(s => s.City == "London");
    }

    [Fact]
    public async Task Handle_FilterByCountryAndCity_ReturnsFilteredStadiums()
    {
        // Arrange
        var stadium1 = TestData.CreateTestDbStadium("LondonEngland");
        stadium1.City = "London";
        stadium1.Country = "England";
        var stadium2 = TestData.CreateTestDbStadium("LondonSpain");
        stadium2.City = "London";
        stadium2.Country = "Spain";
        await _unitOfWork.Stadiums.AddAsync(stadium1);
        await _unitOfWork.Stadiums.AddAsync(stadium2);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetAllStadiumsQuery { Country = "England", City = "London" };

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().OnlyContain(s => s.Country == "England" && s.City == "London");
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllStadiumsQuery { Country = "Nonexistent", City = "Nowhere" };

        // Act
        var result = await _mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums.Should().BeEmpty();
    }
}
