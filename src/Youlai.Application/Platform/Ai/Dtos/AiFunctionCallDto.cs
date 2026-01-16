using System.Text.Json.Serialization;

namespace Youlai.Application.Platform.Ai.Dtos;

/// <summary>
/// AI 函数调用
/// </summary>
public sealed class AiFunctionCallDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; init; }
}
