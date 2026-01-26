using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 用户分页查询参数
/// </summary>
public sealed class UserQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 用户状态
    /// </summary>
    public int? Status { get; init; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DeptId { get; init; }

    /// <summary>
    /// 角色ID集合（逗号分隔）
    /// </summary>
    public string? RoleIds { get; init; }

    /// <summary>
    /// 创建时间区间
    /// </summary>
    public string? CreateTime { get; init; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// 排序方向
    /// </summary>
    public string? Direction { get; init; }
}
