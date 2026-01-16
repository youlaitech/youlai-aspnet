using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Statistics;

/// <summary>
/// 璁块棶瓒嬪娍鏁版嵁
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
