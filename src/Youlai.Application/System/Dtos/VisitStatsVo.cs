using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 访问统计概览
/// </summary>
public sealed class VisitStatsVo
{
    [JsonPropertyName("todayUvCount")]
    public int TodayUvCount { get; init; }

    [JsonPropertyName("totalUvCount")]
    public int TotalUvCount { get; init; }

    [JsonPropertyName("uvGrowthRate")]
    public decimal UvGrowthRate { get; init; }

    [JsonPropertyName("todayPvCount")]
    public int TodayPvCount { get; init; }

    [JsonPropertyName("totalPvCount")]
    public int TotalPvCount { get; init; }

    [JsonPropertyName("pvGrowthRate")]
    public decimal PvGrowthRate { get; init; }
}
