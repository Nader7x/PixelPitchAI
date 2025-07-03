using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Domain.Interfaces;

public interface IRepository<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(int? pageNumber, int? pageSize);

    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(string id);

    Task<EntityEntry<T>> AddAsync(T entity);
    EntityEntry<T> UpdateAsync(T entity);
    EntityEntry<T> DeleteAsync(T entity);
    Task<T?> FindAsync(T entity);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);

    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> GetQueryable();
}
