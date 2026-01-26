using System;
using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Role;

/// <summary>
/// 角色分页查询参数
/// </summary>
/// <remarks>
/// 用于角色分页接口的查询条件。
/// </remarks>
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
