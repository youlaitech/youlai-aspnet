using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 解析请求
/// </summary>
public sealed class AiParseRequestDto
{
    /// <summary>
    /// 用户指令
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>
    /// 当前路由
    /// </summary>
    [JsonPropertyName("currentRoute")]
    public string? CurrentRoute { get; init; }

    /// <summary>
    /// 当前组件
    /// </summary>
    [JsonPropertyName("currentComponent")]
    public string? CurrentComponent { get; init; }

    /// <summary>
    /// 上下文信息
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; init; }
}
