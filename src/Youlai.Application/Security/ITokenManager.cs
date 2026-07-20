using Youlai.Application.Auth.Models;
using Youlai.Application.Security;

namespace Youlai.Application.Security;

/// <summary>
/// JWT 令牌签发、刷新、失效与校验
/// </summary>
public interface ITokenManager
{
    /// <summary>按主体信息签发访问令牌与刷新令牌</summary>
    AuthenticationTokenDto GenerateToken(AuthTokenSubject subject);

    /// <summary>用刷新令牌换取新的访问令牌，刷新令牌本身复用</summary>
    AuthenticationTokenDto RefreshToken(string refreshToken);

    /// <summary>使当前请求携带的令牌失效（登出写入黑名单）</summary>
    Task InvalidateTokenAsync(string? authorizationHeader, CancellationToken cancellationToken = default);

    /// <summary>递增用户令牌版本，使该用户全部历史令牌统一失效（踢下线）</summary>
    Task InvalidateUserSessionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>校验访问令牌签名与版本</summary>
    bool ValidateAccessToken(string token);
}
