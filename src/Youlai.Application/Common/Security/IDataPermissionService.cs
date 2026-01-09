using System.Linq.Expressions;

namespace Youlai.Application.Common.Security;

/// <summary>
/// 数据权限过滤
/// </summary>
public interface IDataPermissionService
{
    /// <summary>
    /// 基于当前用户的数据范围追加过滤条件
    /// </summary>
    IQueryable<TEntity> Apply<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, long>> deptIdSelector,
        Expression<Func<TEntity, long>> userIdSelector);
}
