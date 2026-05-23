using System;
using Youlai.Application.Models;

namespace Youlai.Application.System.Models.Role;

/// <summary>
/// 角色分页查询参数
/// </summary>
public sealed class RoleQuery : BaseQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndDate { get; init; }
}
