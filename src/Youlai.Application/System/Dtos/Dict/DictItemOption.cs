using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀椤逛笅鎷夐€夐」
/// </summary>
public sealed class DictItemOption
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("tagType")]
    public string? TagType { get; init; }
}
