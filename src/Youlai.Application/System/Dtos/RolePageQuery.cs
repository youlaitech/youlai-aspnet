using System;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 角色分页查询参数
/// </summary>
/// <remarks>
/// 用于角色分页接口的查询条件
/// </remarks>
public sealed class RolePageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
