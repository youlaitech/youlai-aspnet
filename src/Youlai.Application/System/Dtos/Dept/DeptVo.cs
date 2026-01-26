using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dept;

/// <summary>
/// 部门数据
/// </summary>
public sealed class DeptVo
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 上级部门ID
    /// </summary>
    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; init; }

    /// <summary>
    /// 子部门
    /// </summary>
    [JsonPropertyName("children")]
    public IReadOnlyCollection<DeptVo>? Children { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
}
