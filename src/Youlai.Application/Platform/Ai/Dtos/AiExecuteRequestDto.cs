using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 执行请求
/// </summary>
public sealed class AiExecuteRequestDto
{
    [JsonPropertyName("parseLogId")]
    public long? ParseLogId { get; init; }

    [JsonPropertyName("originalCommand")]
    public string? OriginalCommand { get; init; }

    [JsonPropertyName("functionCall")]
    public AiFunctionCallDto? FunctionCall { get; init; }

    [JsonPropertyName("confirmMode")]
    public string? ConfirmMode { get; init; }

    [JsonPropertyName("userConfirmed")]
    public bool? UserConfirmed { get; init; }

    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; init; }

    [JsonPropertyName("currentRoute")]
    public string? CurrentRoute { get; init; }
}
