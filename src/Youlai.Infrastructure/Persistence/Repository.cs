using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using Youlai.Application.Persistence;
using Youlai.Domain;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// EF Core 仓储（与 AppDbContext 同生命周期，均 scoped）
/// </summary>
internal sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _ctx;

    public Repository(AppDbContext ctx) => _ctx = ctx;

    public Task<T?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        => _ctx.Set<T>().FindAsync(new object[] { id }, cancellationToken).AsTask();

    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        => _ctx.Set<T>().ToListAsync(cancellationToken);

    public Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _ctx.Set<T>().Where(predicate).ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _ctx.Set<T>().AnyAsync(predicate, cancellationToken);

    public void Add(T entity) => _ctx.Set<T>().Add(entity);
    public void AddRange(IEnumerable<T> entities) => _ctx.Set<T>().AddRange(entities);
    public void Update(T entity) => _ctx.Set<T>().Update(entity);
    public void Remove(T entity) => _ctx.Set<T>().Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => _ctx.Set<T>().RemoveRange(entities);

    /// <summary>批量逻辑删除：直接生成 UPDATE 语句，与原有 ExecuteUpdateAsync 行为一致</summary>
    public async Task<int> BatchSoftDeleteAsync(IEnumerable<long> ids, long? updateBy, CancellationToken cancellationToken = default)
    {
        var idList = ids as List<long> ?? ids.ToList();
        if (idList.Count == 0)
        {
            return 0;
        }

        var query = _ctx.Set<T>().Where(e => idList.Contains(EF.Property<long>(e, "Id")));

        // 版本快照一类实体没有 update_by 列，只写删除标记与更新时间
        if (!typeof(IHasAudit).IsAssignableFrom(typeof(T)))
        {
            return await query.ExecuteUpdateAsync(s => s
                .SetProperty(e => EF.Property<bool>(e, "IsDeleted"), true)
                .SetProperty(e => EF.Property<DateTime?>(e, "UpdateTime"), DateTime.Now), cancellationToken);
        }

        return await query.ExecuteUpdateAsync(s => s
            .SetProperty(e => EF.Property<bool>(e, "IsDeleted"), true)
            .SetProperty(e => EF.Property<long?>(e, "UpdateBy"), updateBy)
            .SetProperty(e => EF.Property<DateTime?>(e, "UpdateTime"), DateTime.Now), cancellationToken);
    }

    /// <summary>批量更新：加载匹配实体后在内存中 mutate，再统一落库</summary>
    public async Task<int> BatchUpdateAsync(Expression<Func<T, bool>> predicate, Action<T> mutate, CancellationToken cancellationToken = default)
    {
        var entities = await _ctx.Set<T>().Where(predicate).ToListAsync(cancellationToken);
        foreach (var entity in entities)
        {
            mutate(entity);
        }

        return await _ctx.SaveChangesAsync(cancellationToken);
    }

    /// <summary>按条件物理删除，走 ExecuteDelete 直接操作数据库，不触发审计与导航加载</summary>
    public Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => _ctx.Set<T>().Where(predicate).ExecuteDeleteAsync(cancellationToken);
}
