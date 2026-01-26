using System.Text.Json.Serialization;

namespace Youlai.Application.System.Dtos.User;

/// <summary>
/// 个人资料表单
/// </summary>
public sealed class UserProfileForm
{
    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; init; }
}
