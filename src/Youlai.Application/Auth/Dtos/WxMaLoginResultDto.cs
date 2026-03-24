namespace Youlai.Application.Auth.Dtos;

/// <summary>
/// 微信小程序登录结果
/// </summary>
public sealed class WxMaLoginResultDto
{
    /// <summary>
    /// 是否新用户
    /// </summary>
    public bool IsNewUser { get; init; }

    /// <summary>
    /// 是否需要绑定手机号
    /// </summary>
    public bool NeedBindMobile { get; init; }

    /// <summary>
    /// 微信openid（绑定手机号时需要）
    /// </summary>
    public string? OpenId { get; init; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int? ExpiresIn { get; init; }

    /// <summary>
    /// 创建需要绑定手机号的结果
    /// </summary>
    public static WxMaLoginResultDto RequireBindMobile(string openId) => new()
    {
        IsNewUser = true,
        NeedBindMobile = true,
        OpenId = openId
    };

    /// <summary>
    /// 创建登录成功的结果
    /// </summary>
    public static WxMaLoginResultDto Success(AuthenticationTokenDto token) => new()
    {
        IsNewUser = false,
        NeedBindMobile = false,
        AccessToken = token.AccessToken,
        RefreshToken = token.RefreshToken,
        TokenType = token.TokenType,
        ExpiresIn = token.ExpiresIn
    };
}
