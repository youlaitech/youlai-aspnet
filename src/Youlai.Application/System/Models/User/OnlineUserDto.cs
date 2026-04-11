using System.Text.Json.Serialization;

namespace Youlai.Application.System.Models.User;

/// <summary>
/// 在线用户信息DTO
/// 用于返回在线用户的基本信息，包括用户名、会话数量和登录时间。
/// </summary>
public class OnlineUserDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 会话数量（多设备登录时大于1）
    /// </summary>
    [JsonPropertyName("sessionCount")]
    public int SessionCount { get; set; }

    /// <summary>
    /// 最早登录时间
    /// </summary>
    [JsonPropertyName("loginTime")]
    public long LoginTime { get; set; }
}
