using Microsoft.EntityFrameworkCore;
using Youlai.Application.Results;

namespace Youlai.Application.Extensions;

/// <summary>
/// IQueryable 分页扩展
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// 分页查询：Count + Skip/Take + PageResult 封装
    /// </summary>
    public static async Task<PageResult<T>> ToPageAsync<T>(
        this IQueryable<T> source,
        int pageNum,
        int pageSize,
        CancellationToken cancellationToken = default)
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
