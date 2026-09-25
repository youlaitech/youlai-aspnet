using System.Text.Json.Serialization;

namespace Youlai.Application.Form.Models;

/// <summary>
/// 表单菜单配置（发布向导回显已生成的菜单）
/// </summary>
public sealed class FormMenuVo
{
    /// <summary>菜单ID</summary>
    [JsonPropertyName("menuId")]
    public long MenuId { get; init; }

    /// <summary>菜单名称</summary>
    [JsonPropertyName("menuName")]
    public string MenuName { get; init; } = string.Empty;

    /// <summary>上级菜单ID</summary>
    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    /// <summary>可见角色ID列表</summary>
    [JsonPropertyName("roleIds")]
    public IReadOnlyCollection<long> RoleIds { get; init; } = Array.Empty<long>();
}
