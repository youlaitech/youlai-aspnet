using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Log;

/// <summary>
/// 鏃ュ織鍒嗛〉鏁版嵁
/// </summary>
public sealed class LogPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("module")]
    public string Module { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("requestUri")]
    public string RequestUri { get; init; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; init; } = string.Empty;

    [JsonPropertyName("browser")]
    public string Browser { get; init; } = string.Empty;

    [JsonPropertyName("os")]
    public string Os { get; init; } = string.Empty;

    [JsonPropertyName("executionTime")]
    public long ExecutionTime { get; init; }

    [JsonPropertyName("operator")]
    public string Operator { get; init; } = string.Empty;
}
