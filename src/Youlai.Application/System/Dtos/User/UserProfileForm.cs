using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 个人资料表单
/// </summary>
public sealed class UserProfileForm
{
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }
}
