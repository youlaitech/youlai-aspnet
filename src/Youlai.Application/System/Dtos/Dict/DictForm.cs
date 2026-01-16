using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 瀛楀吀琛ㄥ崟
/// </summary>
public sealed class DictForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [Required]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [Required]
    [JsonPropertyName("dictCode")]
    public string? DictCode { get; init; }

    [JsonPropertyName("status")]
    [Range(0, 1)]
    public int? Status { get; init; }

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
