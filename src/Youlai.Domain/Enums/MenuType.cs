namespace Youlai.Domain.Enums;

/// <summary>
/// 菜单类型枚举
/// </summary>
public enum MenuType
{
    /// <summary>
    /// 目录
    /// </summary>
    Catalog,

    /// <summary>
    /// 菜单
    /// </summary>
    Menu,

    /// <summary>
    /// 按钮
    /// </summary>
    Button
}

/// <summary>
/// MenuType 扩展方法
/// </summary>
public static class MenuTypeExtensions
{
    /// <summary>
    /// 获取菜单类型代码（数据库存储值）
    /// </summary>
    public static string GetCode(this MenuType type) => type switch
    {
        MenuType.Catalog => "C",
        MenuType.Menu => "M",
        MenuType.Button => "B",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// 从代码解析菜单类型
    /// </summary>
    public static MenuType FromCode(string code) => code?.ToUpperInvariant() switch
    {
        "C" => MenuType.Catalog,
        "M" => MenuType.Menu,
        "B" => MenuType.Button,
        _ => throw new ArgumentException($"Invalid menu type code: {code}", nameof(code))
    };
}
