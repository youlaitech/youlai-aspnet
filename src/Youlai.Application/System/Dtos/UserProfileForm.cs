using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos;

/// <summary>
/// 个人资料表单
/// </summary>
public sealed class UserProfileForm
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
