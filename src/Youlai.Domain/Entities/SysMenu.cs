namespace Youlai.Domain.Entities;

using System.Text.Json;

/// <summary>
/// 菜单实体
/// </summary>
public sealed class SysMenu
{
    /// <summary>主键 ID</summary>
    public long Id { get; set; }

    /// <summary>父菜单 ID（0 表示顶级）</summary>
    public long ParentId { get; set; }

    /// <summary>菜单树路径（逗号分隔的祖先 ID，含自身）</summary>
    public string? TreePath { get; set; }

    /// <summary>菜单名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型（C=目录 M=菜单 E=外链 B=按钮）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>路由名</summary>
    public string? RouteName { get; set; }

    /// <summary>路由路径</summary>
    public string? RoutePath { get; set; }

    /// <summary>组件路径</summary>
    public string? Component { get; set; }

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Perm { get; set; }

    /// <summary>是否缓存（keep-alive）</summary>
    public int? KeepAlive { get; set; }

    /// <summary>是否可见</summary>
    public int? Visible { get; set; }

    /// <summary>排序</summary>
    public int? Sort { get; set; }

    /// <summary>图标</summary>
    public string? Icon { get; set; }

    /// <summary>重定向地址</summary>
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
