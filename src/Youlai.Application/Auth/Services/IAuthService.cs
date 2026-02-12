using Youlai.Application.Auth.Dtos;

namespace Youlai.Application.Auth.Services;

/// <summary>
/// 认证服务
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 登录
    /// </summary>
    Task<AuthenticationTokenDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送登录短信验证码
    /// </summary>
    Task SendSmsLoginCodeAsync(string mobile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    Task<AuthenticationTokenDto> LoginBySmsAsync(string mobile, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<AuthenticationTokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录
    /// </summary>
    Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}
