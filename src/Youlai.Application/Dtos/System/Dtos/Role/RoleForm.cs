using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Role;

/// <summary>
/// 角色表单
/// </summary>
public sealed class RoleForm
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 角色名称
    /// </summary>
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "角色名称不能为空")]
    public string? Name { get; init; }

    /// <summary>
    /// 角色编码
    /// </summary>
    [JsonPropertyName("code")]
    [Required(ErrorMessage = "角色编码不能为空")]
    public string? Code { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    /// <summary>
    /// 状态（0禁用 1启用）
    /// </summary>
    [JsonPropertyName("status")]
    [Range(0, 1, ErrorMessage = "角色状态不正确")]
    public int? Status { get; init; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    [JsonPropertyName("dataScope")]
    public int? DataScope { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; init; }

    [JsonPropertyName("deptIds")]
    public List<long>? DeptIds { get; init; }
}
