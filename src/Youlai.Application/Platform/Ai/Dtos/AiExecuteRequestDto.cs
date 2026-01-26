using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 执行请求
/// </summary>
public sealed class AiExecuteRequestDto
{
    /// <summary>
    /// 解析记录ID
    /// </summary>
    [JsonPropertyName("parseLogId")]
    public long? ParseLogId { get; init; }

    /// <summary>
    /// 原始指令
    /// </summary>
    [JsonPropertyName("originalCommand")]
    public string? OriginalCommand { get; init; }

    /// <summary>
    /// 函数调用信息
    /// </summary>
    [JsonPropertyName("functionCall")]
    public AiFunctionCallDto? FunctionCall { get; init; }

    /// <summary>
    /// 确认模式
    /// </summary>
    [JsonPropertyName("confirmMode")]
    public string? ConfirmMode { get; init; }

    /// <summary>
    /// 用户是否已确认
    /// </summary>
    [JsonPropertyName("userConfirmed")]
    public bool? UserConfirmed { get; init; }

    /// <summary>
    /// 幂等键
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// 当前路由
    /// </summary>
    [JsonPropertyName("currentRoute")]
    public string? CurrentRoute { get; init; }
}
