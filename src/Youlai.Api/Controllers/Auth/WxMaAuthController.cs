using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Auth.Models;
using Youlai.Application.Auth;
using Youlai.Application.Results;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 微信小程序认证接口
/// </summary>
[ApiController]
[Route("api/v1/auth/wxma")]
[Tags("12.微信小程序认证")]
public sealed class WxMaAuthController : ControllerBase
{
    private readonly IWxMaAuthService _wxMaAuthService;

    public WxMaAuthController(IWxMaAuthService wxMaAuthService)
    {
        _wxMaAuthService = wxMaAuthService;
    }

    /// <summary>
    /// 静默登录（已绑定用户返回 token，未绑定用户返回 openId）
    /// </summary>
    /// <param name="code">微信登录凭证（wx.login 获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("silent-login")]
    public async Task<Result<WxMaLoginResultDto>> SilentLogin(
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await _wxMaAuthService.SilentLoginAsync(code, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 手机号快捷登录（企业小程序：一步完成登录并自动创建新用户）
    /// </summary>
    /// <param name="form">登录凭证与手机号授权凭证</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("phone-login")]
    public async Task<Result<AuthenticationTokenDto>> PhoneLogin(
        [FromBody] WxMaPhoneLoginForm form,
        CancellationToken cancellationToken)
    {
        var result = await _wxMaAuthService.PhoneLoginAsync(form.LoginCode, form.PhoneCode, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 绑定手机号（用于静默登录后未绑定手机号的用户；成功后自动登录）
    /// </summary>
    /// <param name="form">登录凭证、手机号与短信验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("bind-mobile")]
    public async Task<Result<AuthenticationTokenDto>> BindMobile(
        [FromBody] WxMaBindMobileForm form,
        CancellationToken cancellationToken)
    {
        var result = await _wxMaAuthService.BindMobileAsync(form.LoginCode, form.Mobile, form.SmsCode, cancellationToken);
        return Result.Success(result);
    }
}
