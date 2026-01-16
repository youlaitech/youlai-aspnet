using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 解析响应
/// </summary>
public sealed class AiParseResponseDto
{
    [JsonPropertyName("parseLogId")]
    public long? ParseLogId { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("functionCalls")]
    public IReadOnlyCollection<AiFunctionCallDto> FunctionCalls { get; init; } = Array.Empty<AiFunctionCallDto>();

    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("rawResponse")]
    public string? RawResponse { get; init; }
}
