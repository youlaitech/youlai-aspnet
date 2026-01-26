using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 菜单表单
/// </summary>
public sealed class MenuForm
{
    /// <summary>
    /// 菜单ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 上级菜单ID
    /// </summary>
    [Required]
    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    [Required]
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
    /// 权限标识
    /// </summary>
    [JsonPropertyName("perm")]
    public string? Perm { get; init; }

    /// <summary>
    /// 是否显示（0否 1是）
    /// </summary>
    [Range(0, 1)]
    [JsonPropertyName("visible")]
    public int? Visible { get; init; }

    /// <summary>
    /// 排序
    /// </summary>
    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

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
    /// 缓存页面
    /// </summary>
    [JsonPropertyName("keepAlive")]
    public int? KeepAlive { get; init; }

    /// <summary>
    /// 始终显示
    /// </summary>
    [JsonPropertyName("alwaysShow")]
    public int? AlwaysShow { get; init; }

    /// <summary>
    /// 路由参数
    /// </summary>
    [JsonPropertyName("params")]
    public IReadOnlyCollection<KeyValue>? Params { get; init; }
}
