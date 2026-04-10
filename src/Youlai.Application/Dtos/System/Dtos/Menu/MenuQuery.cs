using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.Menu;

/// <summary>
/// 菜单查询参数
/// </summary>
public sealed class MenuQuery
{
    /// <summary>
    /// 关键字
    /// </summary>
    [JsonPropertyName("keywords")]
    public string? Keywords { get; init; }

    /// <summary>
    /// 状态
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; init; }
}
