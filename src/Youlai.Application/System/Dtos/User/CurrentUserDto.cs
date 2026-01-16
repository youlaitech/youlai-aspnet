using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 褰撳墠鐧诲綍鐢ㄦ埛淇℃伅
/// </summary>
public sealed class CurrentUserDto
{
    [JsonPropertyName("userId")]
    public long UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; init; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    [JsonPropertyName("roles")]
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    [JsonPropertyName("perms")]
    public IReadOnlyCollection<string> Perms { get; init; } = Array.Empty<string>();
}
