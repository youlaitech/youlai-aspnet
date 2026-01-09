using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 访问趋势数据
/// </summary>
public sealed class VisitTrendVo
{
    [JsonPropertyName("dates")]
    public IReadOnlyCollection<string> Dates { get; init; } = Array.Empty<string>();

    [JsonPropertyName("pvList")]
    public IReadOnlyCollection<int> PvList { get; init; } = Array.Empty<int>();

    [JsonPropertyName("uvList")]
    public IReadOnlyCollection<int> UvList { get; init; } = Array.Empty<int>();

    [JsonPropertyName("ipList")]
    public IReadOnlyCollection<int> IpList { get; init; } = Array.Empty<int>();
}
