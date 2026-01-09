using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Models;

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

    [JsonPropertyName("value")]
    public T? Value { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonPropertyName("children")]
    public IReadOnlyCollection<Option<T>>? Children { get; init; }
}
