using System.Text.Json.Serialization;

namespace Youlai.Application.Common.Models;

/// <summary>
/// 键值对
/// </summary>
public sealed class KeyValue
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
