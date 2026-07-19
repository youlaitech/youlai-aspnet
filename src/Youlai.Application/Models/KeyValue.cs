using System.Text.Json.Serialization;

namespace Youlai.Application.Models;

/// <summary>
/// 键值对
/// </summary>
public sealed class KeyValue
{
    /// <summary>键</summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    /// <summary>值</summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
