namespace Youlai.Application.Common.Services;

/// <summary>
/// 在线用户信息
/// </summary>
public sealed class OnlineUserDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 会话数量（多设备登录）
    /// </summary>
    public int SessionCount { get; init; }

    /// <summary>
    /// 登录时间（毫秒时间戳）
    /// </summary>
    public long LoginTime { get; init; }
}
