using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 解析请求
/// </summary>
public sealed class AiParseRequestDto
{
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    [JsonPropertyName("currentRoute")]
    public string? CurrentRoute { get; init; }

    [JsonPropertyName("currentComponent")]
    public string? CurrentComponent { get; init; }

    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; init; }
}
