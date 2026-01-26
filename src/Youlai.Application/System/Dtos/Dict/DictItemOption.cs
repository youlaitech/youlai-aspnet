using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项下拉选项
/// </summary>
public sealed class DictItemOption
{
    /// <summary>
    /// 选项值
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 选项名称
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 标签类型
    /// </summary>
    [JsonPropertyName("tagType")]
    public string? TagType { get; init; }
}
