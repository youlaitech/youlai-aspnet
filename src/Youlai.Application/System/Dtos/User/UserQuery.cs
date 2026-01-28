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
    public string? Keywords { get; set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DeptId { get; set; }

    /// <summary>
    /// 角色ID集合（逗号分隔）
    /// </summary>
    public string? RoleIds { get; set; }

    /// <summary>
    /// 创建时间区间
    /// </summary>
    public string[]? CreateTime { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// 排序方向
    /// </summary>
    public string? Direction { get; set; }
}
