namespace Youlai.Application.System.Models.Dept;

/// <summary>
/// 部门查询参数
/// </summary>
public sealed class DeptQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// 状态
    /// </summary>
    public int? Status { get; init; }
}
