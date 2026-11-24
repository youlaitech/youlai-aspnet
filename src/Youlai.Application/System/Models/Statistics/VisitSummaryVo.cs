using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Statistics;

/// <summary>
/// 访问汇总
/// </summary>
public sealed class VisitSummaryVo
{
    [JsonPropertyName("todayUv")]
    public int TodayUv { get; init; }

    [JsonPropertyName("yesterdayUv")]
    public int YesterdayUv { get; init; }

    [JsonPropertyName("totalUv")]
    public int TotalUv { get; init; }

    [JsonPropertyName("todayPv")]
    public int TodayPv { get; init; }

    [JsonPropertyName("yesterdayPv")]
    public int YesterdayPv { get; init; }

    [JsonPropertyName("totalPv")]
    public int TotalPv { get; init; }

    /// <summary>
    /// 访客增长率，昨日为 0 时为 null
    /// </summary>
    [JsonPropertyName("uvGrowthRate")]
    public decimal? UvGrowthRate { get; init; }

    /// <summary>
    /// 浏览量增长率，昨日为 0 时为 null
    /// </summary>
    [JsonPropertyName("pvGrowthRate")]
    public decimal? PvGrowthRate { get; init; }
}
