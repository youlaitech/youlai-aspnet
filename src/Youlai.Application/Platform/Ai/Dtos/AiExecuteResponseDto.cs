using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 执行响应
/// </summary>
public sealed class AiExecuteResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// 返回数据
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }

    /// <summary>
    /// 提示信息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// 影响行数
    /// </summary>
    [JsonPropertyName("affectedRows")]
    public int? AffectedRows { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// 记录ID
    /// </summary>
    [JsonPropertyName("recordId")]
    public long? RecordId { get; init; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    [JsonPropertyName("requiresConfirmation")]
    public bool? RequiresConfirmation { get; init; }

    /// <summary>
    /// 确认提示
    /// </summary>
    [JsonPropertyName("confirmationPrompt")]
    public string? ConfirmationPrompt { get; init; }
}
