using System.Text.Json.Serialization;

namespace Youlai.Application.Models;

/// <summary>
/// 导入结果
/// </summary>
public sealed class ExcelResult
{
    /// <summary>结果码，默认 00000（成功）</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = "00000";

    /// <summary>校验不通过的行数</summary>
    [JsonPropertyName("invalidCount")]
    public int InvalidCount { get; init; }

    /// <summary>校验通过的行数</summary>
    [JsonPropertyName("validCount")]
    public int ValidCount { get; init; }

    /// <summary>逐行校验消息</summary>
    [JsonPropertyName("messageList")]
    public IReadOnlyCollection<string> MessageList { get; init; } = Array.Empty<string>();
}
