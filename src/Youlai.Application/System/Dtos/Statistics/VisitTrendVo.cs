using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Statistics;

/// <summary>
/// 访问趋势数据
/// </summary>
public sealed class VisitTrendVo
{
    /// <summary>
    /// 日期列表
    /// </summary>
    [JsonPropertyName("dates")]
    public IReadOnlyCollection<string> Dates { get; init; } = Array.Empty<string>();

    /// <summary>
    /// PV 列表
    /// </summary>
    [JsonPropertyName("pvList")]
    public IReadOnlyCollection<int> PvList { get; init; } = Array.Empty<int>();

    /// <summary>
    /// UV 列表
    /// </summary>
    [JsonPropertyName("uvList")]
    public IReadOnlyCollection<int> UvList { get; init; } = Array.Empty<int>();

    /// <summary>
    /// IP 列表
    /// </summary>
    [JsonPropertyName("ipList")]
    public IReadOnlyCollection<int> IpList { get; init; } = Array.Empty<int>();
}
