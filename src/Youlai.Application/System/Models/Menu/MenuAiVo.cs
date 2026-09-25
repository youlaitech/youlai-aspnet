using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Menu;

/// <summary>
/// AI 推断的菜单配置
/// </summary>
public sealed class MenuAiVo
{
    /// <summary>
    /// 访问路径
    /// </summary>
    [JsonPropertyName("routePath")]
    public string? RoutePath { get; init; }

    /// <summary>
    /// 权限标识
    /// </summary>
    [JsonPropertyName("perm")]
    public string? Perm { get; init; }

    /// <summary>
    /// 建议的图标关键词，按贴切程度排列
    /// </summary>
    [JsonPropertyName("iconKeywords")]
    public IReadOnlyCollection<string>? IconKeywords { get; init; }
}
