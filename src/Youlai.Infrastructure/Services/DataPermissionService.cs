using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 数据权限服务实现
/// </summary>
/// <remarks>
/// 根据当前登录用户的数据权限范围，对查询结果进行过滤
/// 支持多角色数据权限合并（并集策略）
/// </remarks>
internal sealed class DataPermissionService : IDataPermissionService
{
    private readonly ICurrentUser _currentUser;
    private readonly YoulaiDbContext _dbContext;

    public DataPermissionService(ICurrentUser currentUser, YoulaiDbContext dbContext)
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
        // 超级管理员跳过过滤
        if (_currentUser.IsRoot)
        {
            return query;
        }

        var dataScopes = _currentUser.DataScopes;
        
        // 没有数据权限配置，默认只能查看本人数据
        if (dataScopes == null || dataScopes.Count == 0)
        {
            var userId = _currentUser.UserId;
            if (userId is null)
            {
                return query.Where(e => false);
            }
            return query.Where(BuildEquals(userIdSelector, userId.Value));
        }

        // 如果任一角色是 All，则跳过数据权限过滤
        if (HasAllDataScope(dataScopes))
        {
            return query;
        }

        // 多角色数据权限合并（并集策略）
        return ApplyWithDataScopes(query, deptIdSelector, userIdSelector, dataScopes);
    }

    /// <summary>
    /// 判断是否包含"全部数据"权限
    /// </summary>
    private bool HasAllDataScope(IReadOnlyList<RoleDataScope> dataScopes)
    {
        return dataScopes.Any(scope => scope.DataScope == (int)Application.Common.Security.DataScope.All);
    }

    /// <summary>
    /// 应用多角色数据权限（并集策略）
    /// </summary>
    private IQueryable<TEntity> ApplyWithDataScopes<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, long>> deptIdSelector,
        Expression<Func<TEntity, long>> userIdSelector,
        IReadOnlyList<RoleDataScope> dataScopes)
    {
        var userId = _currentUser.UserId;
        var deptId = _currentUser.DeptId;

        // 构建各角色的数据权限条件，使用 OR 连接实现并集
        Expression<Func<TEntity, bool>>? unionExpression = null;

        foreach (var scope in dataScopes)
        {
            var roleExpression = BuildRoleDataScopeExpression(
                deptIdSelector,
                userIdSelector,
                scope,
                userId,
                deptId);

            if (roleExpression != null)
            {
                unionExpression = unionExpression == null
                    ? roleExpression
                    : CombineWithOr(unionExpression, roleExpression);
            }
        }

        if (unionExpression == null)
        {
            // 没有有效权限，不返回任何数据
            return query.Where(e => false);
        }

        return query.Where(unionExpression);
    }

    /// <summary>
    /// 构建单个角色的数据权限表达式
    /// </summary>
    private Expression<Func<TEntity, bool>>? BuildRoleDataScopeExpression<TEntity>(
        Expression<Func<TEntity, long>> deptIdSelector,
        Expression<Func<TEntity, long>> userIdSelector,
        RoleDataScope roleDataScope,
        long? userId,
        long? deptId)
    {
        var dataScope = (Application.Common.Security.DataScope)roleDataScope.DataScope;

        return dataScope switch
        {
            Application.Common.Security.DataScope.All => null, // 全部数据权限，不添加过滤条件
            Application.Common.Security.DataScope.Dept when deptId.HasValue => BuildEquals(deptIdSelector, deptId.Value),
            Application.Common.Security.DataScope.Self when userId.HasValue => BuildEquals(userIdSelector, userId.Value),
            Application.Common.Security.DataScope.DeptAndSub when deptId.HasValue => BuildDeptAndSubExpression(deptIdSelector, deptId.Value),
            Application.Common.Security.DataScope.Custom => BuildCustomDeptExpression(deptIdSelector, roleDataScope.CustomDeptIds),
            _ => userId.HasValue ? BuildEquals(userIdSelector, userId.Value) : null
        };
    }

    /// <summary>
    /// 构建部门及子部门数据权限条件
    /// </summary>
    private Expression<Func<TEntity, bool>> BuildDeptAndSubExpression<TEntity>(
        Expression<Func<TEntity, long>> deptIdSelector,
        long deptId)
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

        return BuildContains(deptIdsQuery, deptIdSelector);
    }

    /// <summary>
    /// 构建自定义部门数据权限条件
    /// </summary>
    private Expression<Func<TEntity, bool>> BuildCustomDeptExpression<TEntity>(
        Expression<Func<TEntity, long>> deptIdSelector,
        List<long>? customDeptIds)
    {
        if (customDeptIds == null || customDeptIds.Count == 0)
        {
            // 没有自定义部门配置，不返回任何数据
            return e => false;
        }

        var deptIdsQuery = _dbContext.SysDepts
            .AsNoTracking()
            .Where(d => !d.IsDeleted && customDeptIds.Contains(d.Id))
            .Select(d => d.Id);

        return BuildContains(deptIdsQuery, deptIdSelector);
    }

    /// <summary>
    /// 构建等于条件
    /// </summary>
    private static Expression<Func<TEntity, bool>> BuildEquals<TEntity>(
        Expression<Func<TEntity, long>> selector,
        long value)
    {
        var parameter = selector.Parameters[0];
        var body = Expression.Equal(selector.Body, Expression.Constant(value));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    /// <summary>
    /// 构建包含条件（IN 子查询）
    /// </summary>
    private static Expression<Func<TEntity, bool>> BuildContains<TEntity>(
        IQueryable<long> values,
        Expression<Func<TEntity, long>> selector)
    {
        var containsMethod = typeof(Queryable)
            .GetMethods()
            .Single(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(long));

        var body = Expression.Call(null, containsMethod, values.Expression, selector.Body);
        return Expression.Lambda<Func<TEntity, bool>>(body, selector.Parameters);
    }

    /// <summary>
    /// 使用 OR 连接两个表达式（并集）
    /// </summary>
    private static Expression<Func<TEntity, bool>> CombineWithOr<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
    {
        var parameter = left.Parameters[0];
        var visitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var rightBody = visitor.Visit(right.Body);

        var orExpression = Expression.OrElse(left.Body, rightBody);
        return Expression.Lambda<Func<TEntity, bool>>(orExpression, parameter);
    }

    /// <summary>
    /// 参数替换访问器
    /// </summary>
    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
