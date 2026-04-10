using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Dept;

/// <summary>
/// 部门表单
/// </summary>
public sealed class DeptForm
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// 上级部门ID
    /// </summary>
    [JsonPropertyName("parentId")]
    [Required(ErrorMessage = "父部门ID不能为空")]
    public long? ParentId { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "状态值不正确")]
    public int? Status { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }
}
