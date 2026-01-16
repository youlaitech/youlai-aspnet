using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀椤硅〃鍗?
/// </summary>
public sealed class DictItemForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("dictCode")]
    public string? DictCode { get; init; }

    [Required]
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [Required]
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1)]
    public int? Status { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("tagType")]
    public string? TagType { get; init; }
}
