using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dict;

/// <summary>
/// 字典分页数据
/// </summary>
public sealed class DictPageVo
{
    /// <summary>
    /// 字典ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 字典名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 字典编码
    /// </summary>
    [JsonPropertyName("dictCode")]
    public string DictCode { get; init; } = string.Empty;

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }
}
