using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 菜单数据
/// </summary>
public sealed class MenuVo
{
    /// <summary>
    /// 菜单ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 上级菜单ID
    /// </summary>
    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 路由名称
    /// </summary>
    [JsonPropertyName("routeName")]
    public string? RouteName { get; init; }

    /// <summary>
    /// 路由路径
    /// </summary>
    [JsonPropertyName("routePath")]
    public string? RoutePath { get; init; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [JsonPropertyName("component")]
    public string? Component { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    /// <summary>
    /// 是否显示（0否 1是）
    /// </summary>
    [JsonPropertyName("visible")]
    public int? Visible { get; init; }

    /// <summary>
    /// 图标
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [JsonPropertyName("redirect")]
    public string? Redirect { get; init; }

    /// <summary>
    /// 权限标识
    /// </summary>
    [JsonPropertyName("perm")]
    public string? Perm { get; init; }

    /// <summary>
    /// 子菜单
    /// </summary>
    [JsonPropertyName("children")]
    public IReadOnlyCollection<MenuVo>? Children { get; init; }
}
