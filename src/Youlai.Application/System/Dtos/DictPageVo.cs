using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 字典分页数据
/// </summary>
public sealed class DictPageVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("dictCode")]
    public string DictCode { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }
}
