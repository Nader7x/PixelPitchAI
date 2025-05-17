using System.Linq.Expressions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;

namespace Infrastructure.Repositories;

public class Repository<T>(FootballDbContext context) : IRepository<T>
    where T : class
{
    private readonly FootballDbContext _context = context;

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
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

    public async Task<T?> FindAsync(Func<T, bool> predicate)
    {
        return await Task.FromResult(_context.Set<T>().FirstOrDefault(predicate));
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
}

