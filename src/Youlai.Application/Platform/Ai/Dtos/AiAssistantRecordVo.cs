using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 助手行为记录
/// </summary>
public sealed class AiAssistantRecordVo
{
    /// <summary>
    /// 记录ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("userId")]
    public long? UserId { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>
    /// 原始指令
    /// </summary>
    [JsonPropertyName("originalCommand")]
    public string? OriginalCommand { get; init; }

    /// <summary>
    /// 模型厂商
    /// </summary>
    [JsonPropertyName("aiProvider")]
    public string? AiProvider { get; init; }

    /// <summary>
    /// 模型名称
    /// </summary>
    [JsonPropertyName("aiModel")]
    public string? AiModel { get; init; }

    /// <summary>
    /// 解析状态
    /// </summary>
    [JsonPropertyName("parseStatus")]
    public int? ParseStatus { get; init; }

    /// <summary>
    /// 函数调用记录
    /// </summary>
    [JsonPropertyName("functionCalls")]
    public string? FunctionCalls { get; init; }

    /// <summary>
    /// 解析说明
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }

    /// <summary>
    /// 可信度
    /// </summary>
    [JsonPropertyName("confidence")]
    public decimal? Confidence { get; init; }

    /// <summary>
    /// 解析失败原因
    /// </summary>
    [JsonPropertyName("parseErrorMessage")]
    public string? ParseErrorMessage { get; init; }

    /// <summary>
    /// 输入 token 数
    /// </summary>
    [JsonPropertyName("inputTokens")]
    public int? InputTokens { get; init; }

    /// <summary>
    /// 输出 token 数
    /// </summary>
    [JsonPropertyName("outputTokens")]
    public int? OutputTokens { get; init; }

    /// <summary>
    /// 解析耗时（毫秒）
    /// </summary>
    [JsonPropertyName("parseDurationMs")]
    public int? ParseDurationMs { get; init; }

    /// <summary>
    /// 函数名称
    /// </summary>
    [JsonPropertyName("functionName")]
    public string? FunctionName { get; init; }

    /// <summary>
    /// 函数参数
    /// </summary>
    [JsonPropertyName("functionArguments")]
    public string? FunctionArguments { get; init; }

    /// <summary>
    /// 执行状态
    /// </summary>
    [JsonPropertyName("executeStatus")]
    public int? ExecuteStatus { get; init; }

    /// <summary>
    /// 执行失败原因
    /// </summary>
    [JsonPropertyName("executeErrorMessage")]
    public string? ExecuteErrorMessage { get; init; }

    /// <summary>
    /// IP 地址
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
}
