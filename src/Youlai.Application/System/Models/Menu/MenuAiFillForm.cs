using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Menu;

/// <summary>
/// AI 推断菜单配置入参
/// </summary>
public sealed class MenuAiFillForm
{
    /// <summary>
    /// 菜单名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 菜单类型（C-目录 M-菜单 E-外链 B-按钮）
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// 上级菜单ID，用于参考同级菜单的命名风格
    /// </summary>
    [JsonPropertyName("parentId")]
    public long? ParentId { get; init; }
}
