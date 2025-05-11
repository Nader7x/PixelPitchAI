using Application.Interfaces;

namespace Infrastructure;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    
    public void Dispose()
    {
       _dbContext.Dispose();
    }

    public async Task<int> CompleteAsync()
    {
       return await _dbContext.SaveChangesAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}