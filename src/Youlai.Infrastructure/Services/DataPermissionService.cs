using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Data;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 数据权限服务实现（对齐 youlai-boot 的 DataScope 枚举）
/// </summary>
/// <remarks>
/// 根据当前登录用户的数据权限范围，对查询结果进行过滤
/// </remarks>
internal sealed class DataPermissionService : IDataPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly AppDbContext _dbContext;

    public DataPermissionService(ICurrentUser currentUser, AppDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 追加数据权限过滤
    /// </summary>
    public IQueryable<TEntity> Apply<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, long>> deptIdSelector,
        Expression<Func<TEntity, long>> userIdSelector)
    {
        if (_currentUser.IsRoot)
        {
            return query;
        }

        var scope = _currentUser.DataScope ?? DataScope.Self;
        if (scope == DataScope.All)
        {
            return query;
        }

        var userId = _currentUser.UserId;
        var deptId = _currentUser.DeptId;
        if (userId is null || deptId is null)
        {
            return query;
        }

        return scope switch
        {
            DataScope.Dept => query.Where(BuildEquals(deptIdSelector, deptId.Value)),
            DataScope.Self => query.Where(BuildEquals(userIdSelector, userId.Value)),
            DataScope.DeptAndSub => ApplyDeptAndSub(query, deptIdSelector, deptId.Value),
            _ => query.Where(BuildEquals(userIdSelector, userId.Value)),
        };
    }

    private IQueryable<TEntity> ApplyDeptAndSub<TEntity>(IQueryable<TEntity> query, Expression<Func<TEntity, long>> deptIdSelector, long deptId)
    {
        var deptIdStr = deptId.ToString();
        var likeMiddle = "%," + deptIdStr + ",%";
        var likeTail = "%," + deptIdStr;
        var likeHead = deptIdStr + ",%";

        var deptIdsQuery = _dbContext.SysDepts
            .AsNoTracking()
            .Where(d => !d.IsDeleted && (d.Id == deptId
                || d.TreePath == deptIdStr
                || EF.Functions.Like(d.TreePath, likeMiddle)
                || EF.Functions.Like(d.TreePath, likeHead)
                || EF.Functions.Like(d.TreePath, likeTail)))
            .Select(d => d.Id);

        return query.Where(BuildContains(deptIdsQuery, deptIdSelector));
    }

    private static Expression<Func<TEntity, bool>> BuildEquals<TEntity>(Expression<Func<TEntity, long>> selector, long value)
    {
        var parameter = selector.Parameters[0];
        var body = Expression.Equal(selector.Body, Expression.Constant(value));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression<Func<TEntity, bool>> BuildContains<TEntity>(IQueryable<long> values, Expression<Func<TEntity, long>> selector)
    {
        var containsMethod = typeof(Queryable)
            .GetMethods()
            .Single(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(long));

        var body = Expression.Call(null, containsMethod, values.Expression, selector.Body);
        return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
    }
}
