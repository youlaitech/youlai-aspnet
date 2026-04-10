using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典表单
/// </summary>
public sealed class DictForm
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 字典名称
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 字典编码
    /// </summary>
    [Required]
    [JsonPropertyName("dictCode")]
    public string? DictCode { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    [Range(0, 1)]
    public int? Status { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; init; }
}
