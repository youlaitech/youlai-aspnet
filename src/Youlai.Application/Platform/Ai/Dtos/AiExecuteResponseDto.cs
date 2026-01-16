using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 执行响应
/// </summary>
public sealed class AiExecuteResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("affectedRows")]
    public int? AffectedRows { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("recordId")]
    public long? RecordId { get; init; }

    [JsonPropertyName("requiresConfirmation")]
    public bool? RequiresConfirmation { get; init; }

    [JsonPropertyName("confirmationPrompt")]
    public string? ConfirmationPrompt { get; init; }
}
