namespace Youlai.Domain.Entities;

using System.Text.Json;

/// <summary>
/// 菜单实体
/// </summary>
public sealed class SysMenu
{
    public long Id { get; set; }

    public long ParentId { get; set; }

    public string? TreePath { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型（M=目录 C=菜单 F=按钮）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string? RouteName { get; set; }

    public string? RoutePath { get; set; }

    public string? Component { get; set; }

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Perm { get; set; }

    public int? AlwaysShow { get; set; }

    public int? KeepAlive { get; set; }

    public int? Visible { get; set; }

    public int? Sort { get; set; }

    public string? Icon { get; set; }

    public string? Redirect { get; set; }

    /// <summary>
    /// 路由参数
    /// </summary>
    public JsonElement? Params { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
}
