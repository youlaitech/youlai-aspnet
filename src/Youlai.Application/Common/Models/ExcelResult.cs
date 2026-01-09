using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Models;

/// <summary>
/// 导入结果
/// </summary>
public sealed class ExcelResult
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = "00000";

    [JsonPropertyName("invalidCount")]
    public int InvalidCount { get; init; }

    [JsonPropertyName("validCount")]
    public int ValidCount { get; init; }

    [JsonPropertyName("messageList")]
    public IReadOnlyCollection<string> MessageList { get; init; } = Array.Empty<string>();
}
