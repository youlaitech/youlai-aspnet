using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 助手行为记录
/// </summary>
public sealed class AiAssistantRecordVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("userId")]
    public long? UserId { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("originalCommand")]
    public string? OriginalCommand { get; init; }

    [JsonPropertyName("aiProvider")]
    public string? AiProvider { get; init; }

    [JsonPropertyName("aiModel")]
    public string? AiModel { get; init; }

    [JsonPropertyName("parseStatus")]
    public int? ParseStatus { get; init; }

    [JsonPropertyName("functionCalls")]
    public string? FunctionCalls { get; init; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }

    [JsonPropertyName("confidence")]
    public decimal? Confidence { get; init; }

    [JsonPropertyName("parseErrorMessage")]
    public string? ParseErrorMessage { get; init; }

    [JsonPropertyName("inputTokens")]
    public int? InputTokens { get; init; }

    [JsonPropertyName("outputTokens")]
    public int? OutputTokens { get; init; }

    [JsonPropertyName("parseDurationMs")]
    public int? ParseDurationMs { get; init; }

    [JsonPropertyName("functionName")]
    public string? FunctionName { get; init; }

    [JsonPropertyName("functionArguments")]
    public string? FunctionArguments { get; init; }

    [JsonPropertyName("executeStatus")]
    public int? ExecuteStatus { get; init; }

    [JsonPropertyName("executeErrorMessage")]
    public string? ExecuteErrorMessage { get; init; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }

    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }

    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
}
