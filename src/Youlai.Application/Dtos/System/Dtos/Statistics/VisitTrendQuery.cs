namespace Youlai.Application.System.Dtos.Statistics;

/// <summary>
/// 访问趋势查询参数
/// </summary>
public sealed class VisitTrendQuery
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// 结束日期
    /// </summary>
    public string EndDate { get; init; } = string.Empty;
}
