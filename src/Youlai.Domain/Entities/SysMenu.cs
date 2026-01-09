namespace Youlai.Domain.Entities;

/// <summary>
/// 菜单
/// </summary>
public sealed class SysMenu
{
    /// <summary>
    /// 主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long ParentId { get; set; }

    /// <summary>
    /// 树路径
    /// </summary>
    public string? TreePath { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 路由名称
    /// </summary>
    public string? RouteName { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 前端组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Perm { get; set; }

    /// <summary>
    /// 始终显示
    /// </summary>
    public int? AlwaysShow { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public int? KeepAlive { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public int? Visible { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 重定向
    /// </summary>
    public string? Redirect { get; set; }

    /// <summary>
    /// 路由参数
    /// </summary>
    public string? Params { get; set; }
}
