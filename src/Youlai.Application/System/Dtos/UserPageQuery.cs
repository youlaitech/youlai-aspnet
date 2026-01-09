namespace Youlai.Application.System.Dtos;

/// <summary>
/// 用户分页查询参数
/// </summary>
public sealed class UserPageQuery
{
    public int PageNum { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keywords { get; init; }

    public int? Status { get; init; }

    public long? DeptId { get; init; }

    public string? RoleIds { get; init; }

    public string? CreateTime { get; init; }

    public string? Field { get; init; }

    public string? Direction { get; init; }
}
