using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 鑿滃崟鏌ヨ鍙傛暟
/// </summary>
public sealed class MenuQuery
{
    [JsonPropertyName("keywords")]
    public string? Keywords { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }
}
