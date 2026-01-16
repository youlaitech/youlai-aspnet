using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 鑿滃崟鏁版嵁
/// </summary>
public sealed class MenuVo
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("routeName")]
    public string? RouteName { get; init; }

    [JsonPropertyName("routePath")]
    public string? RoutePath { get; init; }

    [JsonPropertyName("component")]
    public string? Component { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("visible")]
    public int? Visible { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("redirect")]
    public string? Redirect { get; init; }

    [JsonPropertyName("perm")]
    public string? Perm { get; init; }

    [JsonPropertyName("children")]
    public IReadOnlyCollection<MenuVo>? Children { get; init; }
}
