using System.Text.Json.Serialization;

namespace Youlai.Application.Models;

/// <summary>
/// 下拉选项
/// </summary>
public sealed class Option<T>
{
    public Option()
    {
    }

    public Option(T value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>选项值</summary>
    [JsonPropertyName("value")]
    public T? Value { get; init; }

    /// <summary>显示文本</summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>标签/颜色标记，可选</summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    /// <summary>子选项，用于级联</summary>
    [JsonPropertyName("children")]
    public IReadOnlyCollection<Option<T>>? Children { get; init; }
}
