using Youlai.Application.Auth.Models;

namespace Youlai.Application.Auth;

/// <summary>
/// 认证服务
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 登录
    /// </summary>
    Task<(AuthenticationTokenDto Token, long UserId)> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送登录短信验证码
    /// </summary>
    Task SendSmsLoginCodeAsync(string mobile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    Task<(AuthenticationTokenDto Token, long UserId)> LoginBySmsAsync(string mobile, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<AuthenticationTokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录
    /// </summary>
    Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default);

    /// <summary>
    /// 扫码登录换发会话令牌：按用户 ID 查认证信息并复用既有令牌签发逻辑。
    /// </summary>
    Task<AuthenticationTokenDto> LoginByQrAsync(long userId, CancellationToken cancellationToken = default);
}
