using System.Linq.Expressions;
using Domain.Interfaces;
using Domain.Models;
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
                .Take(pageSize.Value)
                .ToListAsync();
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<PagedList<T>> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes
    )
    {
        var query = _context.Set<T>().AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);
        if (includes.Length != 0)
            query = includes.Aggregate(query, (current, include) => current.Include(include));

        return await PagedList<T>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<EntityEntry<T>> AddAsync(T entity)
    {
        return await _context.AddAsync(entity);
    }

    public EntityEntry<T> Update(T entity)
    {
        return _context.Set<T>().Update(entity);
    }

    public async Task<int> UpdateAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TProperty>> propertySelector,
        TProperty newValue
    )
    {
        return await _context
            .Set<T>()
            .Where(predicate)
            .ExecuteUpdateAsync(setter => setter.SetProperty(propertySelector, newValue));
    }

    public async Task<int> UpdateAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, Expression<Func<T, TProperty>>>> propertySelector,
        Expression<Func<T, TProperty>> valueExpression
    )
    {
        return await _context
            .Set<T>()
            .Where(predicate)
            .ExecuteUpdateAsync(setter => setter.SetProperty(propertySelector, valueExpression));
    }

    public EntityEntry<T> Delete(T entity)
    {
        return _context.Remove(entity);
    }

    public async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ExecuteDeleteAsync();
    }

    public async Task<T?> FindAsync(T entity)
    {
        var primaryKey = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey();

        if (primaryKey == null)
            throw new InvalidOperationException(
                $"Entity type {typeof(T).Name} does not have a primary key defined."
            );

        var keyValues = primaryKey
            .Properties.Select(p => p.GetGetter().GetClrValue(entity))
            .ToArray();

        return await _context.Set<T>().FindAsync(keyValues);
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
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes
    )
    {
        IQueryable<T> query = _context.Set<T>();
        if (predicate != null)
            query = query.Where(predicate);

        return includes.Aggregate(query, (current, include) => current.Include(include));
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Set<T>().AnyAsync(predicate, cancellationToken);
    }

    public async Task<List<T?>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Get the unique IDs to query the database efficiently
        var uniqueIds = ids.Distinct().ToList();

        var results = await _context
            .Set<T>()
            .Where(entity => uniqueIds.Contains(EF.Property<int>(entity, "Id")))
            .Select(entity => new { Id = EF.Property<int>(entity, "Id"), Entity = entity })
            .ToListAsync(cancellationToken);

        var entitiesById = results.ToDictionary(r => r.Id, r => r.Entity);
        return ids.Select(id => entitiesById.TryGetValue(id, out var entity) ? entity : null)
            .ToList();
    }

    public async Task<bool> AddRangeAsync(
        CancellationToken cancellationToken = default,
        params T[] entities
    )
    {
        if (entities.Length == 0)
            return false;

        await _context.Set<T>().AddRangeAsync(entities, cancellationToken);
        return true;
    }
}
