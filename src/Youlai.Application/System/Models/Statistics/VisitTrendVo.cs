using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Statistics;

/// <summary>
/// 单日访问量
/// </summary>
public sealed class VisitTrendPointVo
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("pv")]
    public int Pv { get; init; }

    [JsonPropertyName("uv")]
    public int Uv { get; init; }
}

/// <summary>
/// 访问趋势数据
/// </summary>
public sealed class VisitTrendVo
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; init; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; init; } = string.Empty;

    [JsonPropertyName("points")]
    public IReadOnlyCollection<VisitTrendPointVo> Points { get; init; } = Array.Empty<VisitTrendPointVo>();
}
