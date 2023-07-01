using Ordering.Domain.Common;
using System.Linq.Expressions;

namespace Ordering.Application.Contracts.Persistence
{
    public interface IRepositoryBase<TEntity> where TEntity : EntityBase
    {
        Task<IReadOnlyList<TEntity>> GetAllAsync();

        Task<IReadOnlyList<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate);

        Task<IReadOnlyList<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate = null,
                                        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                        string includeString = null,
                                        bool disableTracking = true);

        Task<IReadOnlyList<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate = null,
                                       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                                       List<Expression<Func<TEntity, object>>> includes = null,
                                       bool disableTracking = true);
        Task<TEntity?> GetByIdAsync(int id);

        Task<TEntity> AddAsync(TEntity entity);

        Task UpdateAsync(TEntity entity);

        Task DeleteAsync(TEntity entity);
    }
}
