using Microsoft.EntityFrameworkCore;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Persistence;

/// <summary>
/// 基于 EF Core 的只读上下文实现；Query 返回不跟踪、已应用全局过滤的 IQueryable
/// </summary>
internal sealed class EfReadOnlyDbContext : IReadOnlyDbContext
{
    private readonly AppDbContext _ctx;

    public EfReadOnlyDbContext(AppDbContext ctx) => _ctx = ctx;

    public IQueryable<T> Query<T>() where T : class => _ctx.Set<T>().AsNoTracking();

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.ToListAsync(cancellationToken);

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.CountAsync(cancellationToken);

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.AnyAsync(cancellationToken);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.FirstOrDefaultAsync(cancellationToken);

    public Task<PageResult<T>> PageAsync<T>(IQueryable<T> query, int pageNum, int pageSize, CancellationToken cancellationToken = default)
        => QueryablePageAsync(query, pageNum, pageSize, cancellationToken);

    private static async Task<PageResult<T>> QueryablePageAsync<T>(IQueryable<T> source, int pageNum, int pageSize, CancellationToken cancellationToken)
    {
        var total = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<T> list;
        if (total == 0)
        {
            list = Array.Empty<T>();
        }
        else
        {
            list = await source
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return PageResult<T>.Success(list, total, pageNum, pageSize);
    }
}
