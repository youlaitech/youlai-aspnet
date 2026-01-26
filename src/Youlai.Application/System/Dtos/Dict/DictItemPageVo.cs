using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项分页数据
/// </summary>
public sealed class DictItemPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("dictCode")]
    public string DictCode { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }
}
