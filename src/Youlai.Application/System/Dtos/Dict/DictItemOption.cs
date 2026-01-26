using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项下拉选项
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
