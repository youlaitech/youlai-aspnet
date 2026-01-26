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
    public string? Keywords { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
