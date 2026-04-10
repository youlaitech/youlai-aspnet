using Youlai.Core.Auth.Dtos;

namespace Youlai.Core.Auth.Services;

/// <summary>
/// 微信小程序认证服务
/// </summary>
public interface IWxMaAuthService
{
    /// <summary>
    /// 静默登录
    /// <para>
    /// 通过微信登录凭证（code）获取用户唯一标识（openid），
    /// 如果用户已绑定手机号则直接登录成功，否则返回需绑定手机号的提示。
    /// </para>
    /// </summary>
    /// <param name="code">微信登录凭证（wx.login 获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果（成功返回 token，需绑定返回 openid）</returns>
    Task<WxMaLoginResultDto> SilentLoginAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手机号快捷登录
    /// <para>
    /// 同时使用微信登录凭证和手机号授权凭证，
    /// 一步完成用户注册/登录，无需额外绑定流程。
    /// 适用于企业认证的小程序（已开通手机号快捷登录权限）。
    /// </para>
    /// </summary>
    /// <param name="loginCode">微信登录凭证（wx.login 获取）</param>
    /// <param name="phoneCode">手机号授权凭证（getPhoneNumber 事件获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证令牌</returns>
    Task<AuthenticationTokenDto> PhoneLoginAsync(string loginCode, string phoneCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 绑定手机号
    /// <para>
    /// 为已静默登录但未绑定手机号的用户绑定手机号，
    /// 绑定成功后自动完成登录。
    /// </para>
    /// </summary>
    /// <param name="openId">微信用户唯一标识</param>
    /// <param name="mobile">手机号码</param>
    /// <param name="smsCode">短信验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证令牌</returns>
    Task<AuthenticationTokenDto> BindMobileAsync(string openId, string mobile, string smsCode, CancellationToken cancellationToken = default);
}
