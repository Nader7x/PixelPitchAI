using System.Linq.Expressions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Repositories;

public class Repository<T>(FootballDbContext context) : IRepository<T>
    where T : class
{
    private readonly FootballDbContext _context = context;

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync(int? pageNumber, int? pageSize)
    {
        if (pageNumber > 0 && pageSize > 0)
            return await _context
                .Set<T>()
                .AsNoTracking()
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value * 3)
                .ToListAsync();
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<EntityEntry<T>> AddAsync(T entity)
    {
        return await _context.AddAsync(entity);
    }

    public EntityEntry<T> UpdateAsync(T entity)
    {
        return _context.Set<T>().Update(entity);
    }

    public EntityEntry<T> DeleteAsync(T entity)
    {
        return _context.Remove(entity);
    }

    public async Task<T?> FindAsync(T entity)
    {
        return await _context.Set<T>().FindAsync(entity);
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await Task.FromResult(_context.Set<T>().FirstOrDefault(predicate));
    }

    public async Task<T?> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    )
    {
        return await _context
            .Set<T>()
            .AsQueryable()
            .FirstOrDefaultAsync(predicate, cancellationToken);
        ;
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().CountAsync(predicate);
    }

    public IQueryable<T> GetQueryable()
    {
        return _context.Set<T>();
    }

    public IQueryable<T> GetQueryable(
        Expression<Func<T, bool>> predicate = null,
        params Expression<Func<T, object>>[] includes
    )
    {
        IQueryable<T> query = _context.Set<T>();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }
}
