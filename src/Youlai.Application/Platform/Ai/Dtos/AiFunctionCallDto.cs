using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 函数调用
/// </summary>
public sealed class AiFunctionCallDto
{
    /// <summary>
    /// 函数名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 函数说明
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// 函数参数
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; init; }
}
