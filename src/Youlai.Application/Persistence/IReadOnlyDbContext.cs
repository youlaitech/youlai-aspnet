using System.Linq.Expressions;
using Youlai.Application.Results;

namespace Youlai.Application.Persistence;

/// <summary>
/// 只读数据访问。Query 方法返回的 IQueryable 已应用全局过滤且不跟踪
/// </summary>
public interface IReadOnlyDbContext
{
    /// <summary>返回实体的只读 IQueryable（已应用逻辑删除过滤、不跟踪）</summary>
    IQueryable<T> Query<T>() where T : class;

    /// <summary>执行查询并物化为列表</summary>
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>执行查询并计数</summary>
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>判断查询是否存在匹配项</summary>
    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>取查询的首条结果，无匹配返回 null</summary>
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>分页：先计数再 Skip/Take 取页，返回 PageResult</summary>
    Task<PageResult<T>> PageAsync<T>(IQueryable<T> query, int pageNum, int pageSize, CancellationToken cancellationToken = default);
}
