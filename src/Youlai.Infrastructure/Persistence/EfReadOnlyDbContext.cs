using Microsoft.EntityFrameworkCore;
using Youlai.Application.Persistence;
using Youlai.Application.Results;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// IReadOnlyDbContext 的 EF Core 实现，委托 AppDbContext 完成实际查询
/// </summary>
internal sealed class EfReadOnlyDbContext : IReadOnlyDbContext
{
    private readonly AppDbContext _context;

    public EfReadOnlyDbContext(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 返回实体的不跟踪 IQueryable，已应用全局查询过滤
    /// </summary>
    public IQueryable<T> Query<T>() where T : class
    {
        return _context.Set<T>().AsNoTracking();
    }

    /// <summary>
    /// 执行查询并物化为 List
    /// </summary>
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        return query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 执行查询并返回计数
    /// </summary>
    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        return query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// 执行查询，存在匹配项返回 true
    /// </summary>
    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        return query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 取首条结果，无匹配返回 null
    /// </summary>
    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 分页查询：先 COUNT 再 Skip/Take 取页
    /// </summary>
    public async Task<PageResult<T>> PageAsync<T>(
        IQueryable<T> query,
        int pageNum,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((pageNum - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return PageResult<T>.Success(items, total, pageNum, pageSize);
    }
}
