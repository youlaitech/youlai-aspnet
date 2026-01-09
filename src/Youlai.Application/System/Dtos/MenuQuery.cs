using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 菜单查询参数
/// </summary>
public sealed class MenuQuery
{
    [JsonPropertyName("keywords")]
    public string? Keywords { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }
}
