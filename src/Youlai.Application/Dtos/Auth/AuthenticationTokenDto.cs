namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 令牌信息
/// </summary>
/// <param name="TokenType">令牌类型</param>
/// <param name="AccessToken">访问令牌</param>
/// <param name="RefreshToken">刷新令牌</param>
/// <param name="ExpiresIn">过期时间（秒）</param>
public sealed record AuthenticationTokenDto(
    string TokenType,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);
