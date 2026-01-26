namespace Youlai.Application.System.Dtos.Statistics;

/// <summary>
/// 访问趋势查询参数
/// </summary>
public sealed class VisitTrendQuery
{
    public string StartDate { get; init; } = string.Empty;

    public string EndDate { get; init; } = string.Empty;
}
