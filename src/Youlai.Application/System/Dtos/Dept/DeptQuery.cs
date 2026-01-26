namespace Youlai.Application.System.Dtos.Dept;

/// <summary>
/// 部门查询参数
/// </summary>
public sealed class DeptQuery
{
    public string? Keywords { get; init; }

    public int? Status { get; init; }
}
