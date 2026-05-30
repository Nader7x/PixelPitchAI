using System.Linq.Expressions;
using Domain.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Domain.Interfaces;

public interface IRepository<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(int? pageNumber, int? pageSize);
    Task<PagedList<T>> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes
    );

    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<T?> GetByIdAsync(string id);

    Task<EntityEntry<T>> AddAsync(T entity);
    EntityEntry<T> Update(T entity);

    Task<int> UpdateAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TProperty>> propertySelector,
        TProperty newValue
    );

    Task<int> UpdateAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, Expression<Func<T, TProperty>>>> propertySelector,
        Expression<Func<T, TProperty>> valueExpression
    );

    EntityEntry<T> Delete(T entity);
    Task<int> DeleteAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindAsync(T entity);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> GetQueryable();

    IQueryable<T> GetQueryable(
        Expression<Func<T, bool>>? predicate = null,
        params Expression<Func<T, object>>[] includes
    );

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    );
    Task<List<T?>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default
    );
    Task<bool> AddRangeAsync(CancellationToken cancellationToken = default, params T[] entities);
}
