using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 当前登录用户信息
/// </summary>
public sealed class CurrentUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("userId")]
    public long UserId { get; init; }

    /// <summary>
    /// 登录账号
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; init; } = string.Empty;

    /// <summary>
    /// 头像地址
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    /// <summary>
    /// 角色列表
    /// </summary>
    [JsonPropertyName("roles")]
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 权限列表
    /// </summary>
    [JsonPropertyName("perms")]
    public IReadOnlyCollection<string> Perms { get; init; } = Array.Empty<string>();
}
