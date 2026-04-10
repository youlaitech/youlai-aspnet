using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项表单
/// </summary>
public sealed class DictItemForm
{
    /// <summary>
    /// 字典项ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 字典编码
    /// </summary>
    [JsonPropertyName("dictCode")]
    public string? DictCode { get; init; }

    /// <summary>
    /// 字典项名称
    /// </summary>
    [Required]
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    /// <summary>
    /// 字典项值
    /// </summary>
    [Required]
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    [Range(0, 1)]
    public int? Status { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    /// <summary>
    /// 标签类型
    /// </summary>
    [JsonPropertyName("tagType")]
    public string? TagType { get; init; }
}
