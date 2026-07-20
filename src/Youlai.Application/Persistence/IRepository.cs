using System.Linq.Expressions;

namespace Youlai.Application.Persistence;

/// <summary>
/// 仓储端口：封装单实体的写操作，业务不直接调用 SaveChanges
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>批量逻辑删除：按 id 集合置 IsDeleted，并补更新人/时间</summary>
    Task<int> BatchSoftDeleteAsync(IEnumerable<long> ids, long? updateBy, CancellationToken cancellationToken = default);

    /// <summary>批量更新：加载匹配实体后在内存中 mutate，再统一落库</summary>
    Task<int> BatchUpdateAsync(Expression<Func<T, bool>> predicate, Action<T> mutate, CancellationToken cancellationToken = default);
}
