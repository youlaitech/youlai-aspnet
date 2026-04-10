using System.Text.Json.Serialization;

namespace Youlai.Core.System.Dtos.Log;

/// <summary>
/// 日志分页数据
/// </summary>
public sealed class LogPageVo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("module")]
    public string? Module { get; init; }

    [JsonPropertyName("actionType")]
    public string? ActionType { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("operatorId")]
    public string? OperatorId { get; init; }

    [JsonPropertyName("operatorName")]
    public string? OperatorName { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("requestUri")]
    public string? RequestUri { get; init; }

    [JsonPropertyName("requestMethod")]
    public string? RequestMethod { get; init; }

    [JsonPropertyName("ip")]
    public string? Ip { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("device")]
    public string? Device { get; init; }

    [JsonPropertyName("browser")]
    public string? Browser { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }

    [JsonPropertyName("executionTime")]
    public int? ExecutionTime { get; init; }

    [JsonPropertyName("errorMsg")]
    public string? ErrorMsg { get; init; }

    [JsonPropertyName("createTime")]
    public DateTime? CreateTime { get; init; }
}
