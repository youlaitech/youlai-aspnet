namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 令牌信息
/// </summary>
public sealed record AuthenticationTokenDto(
    string TokenType,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn
);
