using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单菜单发布对象（发布向导入口配置步骤提交）
/// </summary>
public sealed class FormMenuForm
{
    /// <summary>菜单名称（侧边栏展示）</summary>
    [Required(ErrorMessage = "菜单名称不能为空")]
    [StringLength(50, ErrorMessage = "菜单名称不能超过 50 个字符")]
    [JsonPropertyName("menuName")]
    public string MenuName { get; init; } = string.Empty;

    /// <summary>上级菜单ID（须为目录类型；为空时自动使用/创建默认目录"表单中心"）</summary>
    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }

    /// <summary>可见角色ID列表</summary>
    [JsonPropertyName("roleIds")]
    public IReadOnlyCollection<long>? RoleIds { get; init; }
}
