using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 解析响应
/// </summary>
public sealed class AiParseResponseDto
{
    /// <summary>
    /// 解析记录ID
    /// </summary>
    [JsonPropertyName("parseLogId")]
    public long? ParseLogId { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// 函数调用列表
    /// </summary>
    [JsonPropertyName("functionCalls")]
    public IReadOnlyCollection<AiFunctionCallDto> FunctionCalls { get; init; } = Array.Empty<AiFunctionCallDto>();

    /// <summary>
    /// 解析说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }

    /// <summary>
    /// 可信度
    /// </summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// 原始响应
    /// </summary>
    [JsonPropertyName("rawResponse")]
    public string? RawResponse { get; init; }
}
