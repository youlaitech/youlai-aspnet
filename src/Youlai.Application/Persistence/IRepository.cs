using System.Linq.Expressions;

namespace Youlai.Application.Persistence;

/// <summary>
/// 单实体增删改查，写操作由 IUnitOfWork.SaveChangesAsync 统一落库
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>加载全部实体（跟踪），用于内存中变更后统一落库</summary>
    Task<List<T>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>按条件加载实体（跟踪），用于内存中变更后统一落库</summary>
    Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

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

    /// <summary>按条件物理删除，走 ExecuteDelete 直接操作数据库，不触发审计与导航加载</summary>
    Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
