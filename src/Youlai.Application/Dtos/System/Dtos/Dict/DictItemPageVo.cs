using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典项分页数据
/// </summary>
public sealed class DictItemPageVo
{
    /// <summary>
    /// 字典项ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 字典编码
    /// </summary>
    [JsonPropertyName("dictCode")]
    public string DictCode { get; init; } = string.Empty;

    /// <summary>
    /// 字典项名称
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 字典项值
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }
}
