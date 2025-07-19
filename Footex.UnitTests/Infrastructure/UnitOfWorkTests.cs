using System.Reflection;
using Domain.Models;
using Domain.Repositories;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure;

public class UnitOfWorkTests : IDisposable
{
    private readonly FootballDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<FootballDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new FootballDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<ApplicationUser>>().Object,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object
        );

        var stadiumsRepositoryMock = new Mock<IStadiumsRepository>();

        _unitOfWork = new UnitOfWork(
            _context,
            userManagerMock.Object,
            stadiumsRepositoryMock.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCallContextSaveChangesAsync()
    {
        // Arrange
        var team = new Team { Name = "Test Team" };
        _context.Teams.Add(team);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldStartTransaction()
    {
        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync();

        // Assert
        Assert.NotNull(transaction);
        Assert.IsAssignableFrom<IDbContextTransaction>(transaction);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithActiveTransaction_ShouldCommit()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var transactionMock = new Mock<IDbContextTransaction>();
        var field = typeof(UnitOfWork).GetField(
            "_transaction",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.NotNull(field);
        field.SetValue(_unitOfWork, transactionMock.Object);

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithActiveTransaction_ShouldRollback()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var transactionMock = new Mock<IDbContextTransaction>();
        var field = typeof(UnitOfWork).GetField(
            "_transaction",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.NotNull(field);
        field.SetValue(_unitOfWork, transactionMock.Object);

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
