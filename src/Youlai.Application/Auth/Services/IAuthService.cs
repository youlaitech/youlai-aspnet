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
    /// 刷新令牌
    /// </summary>
    Task<AuthenticationTokenDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录
    /// </summary>
    Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}
