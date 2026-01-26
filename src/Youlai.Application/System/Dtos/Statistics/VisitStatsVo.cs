using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Statistics;

/// <summary>
/// 访问统计概览
/// </summary>
public sealed class VisitStatsVo
{
    /// <summary>
    /// 今日 UV
    /// </summary>
    [JsonPropertyName("todayUvCount")]
    public int TodayUvCount { get; init; }

    /// <summary>
    /// 累计 UV
    /// </summary>
    [JsonPropertyName("totalUvCount")]
    public int TotalUvCount { get; init; }

    /// <summary>
    /// UV 增长率
    /// </summary>
    [JsonPropertyName("uvGrowthRate")]
    public decimal UvGrowthRate { get; init; }

    /// <summary>
    /// 今日 PV
    /// </summary>
    [JsonPropertyName("todayPvCount")]
    public int TodayPvCount { get; init; }

    /// <summary>
    /// 累计 PV
    /// </summary>
    [JsonPropertyName("totalPvCount")]
    public int TotalPvCount { get; init; }

    /// <summary>
    /// PV 增长率
    /// </summary>
    [JsonPropertyName("pvGrowthRate")]
    public decimal PvGrowthRate { get; init; }
}
