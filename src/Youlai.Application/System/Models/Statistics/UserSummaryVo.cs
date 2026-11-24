using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.Statistics;

/// <summary>
/// 用户汇总
/// </summary>
public sealed class UserSummaryVo
{
    [JsonPropertyName("total")]
    public int Total { get; init; }
}
