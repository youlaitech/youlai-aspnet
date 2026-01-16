using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Youlai.Application.Common.Models;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 鑿滃崟琛ㄥ崟
/// </summary>
public sealed class MenuForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [Required]
    [JsonPropertyName("parentId")]
    public long ParentId { get; init; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("routeName")]
    public string? RouteName { get; init; }

    [JsonPropertyName("routePath")]
    public string? RoutePath { get; init; }

    [JsonPropertyName("component")]
    public string? Component { get; init; }

    [JsonPropertyName("perm")]
    public string? Perm { get; init; }

    [Range(0, 1)]
    [JsonPropertyName("visible")]
    public int? Visible { get; init; }

    [JsonPropertyName("sort")]
    public int? Sort { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("redirect")]
    public string? Redirect { get; init; }

    [JsonPropertyName("keepAlive")]
    public int? KeepAlive { get; init; }

    [JsonPropertyName("alwaysShow")]
    public int? AlwaysShow { get; init; }

    [JsonPropertyName("params")]
    public IReadOnlyCollection<KeyValue>? Params { get; init; }
}
