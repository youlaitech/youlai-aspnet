using Youlai.Application.Auth.Models;

namespace Youlai.Application.Auth;

/// <summary>
/// 微信小程序认证服务
/// </summary>
public interface IWxMaAuthService
{
    /// <summary>
    /// 静默登录：凭证换 openid，已绑手机号返回令牌，未绑返回待绑定提示
    /// </summary>
    /// <param name="code">微信登录凭证（wx.login 获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录结果（成功返回 token，需绑定时 needBindMobile 为 true）</returns>
    Task<WxMaLoginResultDto> SilentLoginAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手机号快捷登录：登录凭证与手机号授权凭证一次完成注册/登录，需企业认证小程序
    /// </summary>
    /// <param name="loginCode">微信登录凭证（wx.login 获取）</param>
    /// <param name="phoneCode">手机号授权凭证（getPhoneNumber 事件获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证令牌</returns>
    Task<AuthenticationTokenDto> PhoneLoginAsync(string loginCode, string phoneCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为静默登录但未绑定手机号的用户绑定手机号并完成登录
    /// </summary>
    /// <param name="loginCode">提交绑定前重新获取的微信登录凭证</param>
    /// <param name="mobile">手机号码</param>
    /// <param name="smsCode">短信验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证令牌</returns>
    Task<AuthenticationTokenDto> BindMobileAsync(string loginCode, string mobile, string smsCode, CancellationToken cancellationToken = default);
}
