using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 微信小程序认证接口
/// </summary>
[ApiController]
[Route("api/v1/wechat/miniapp/auth")]
[Tags("13.微信小程序认证")]
public sealed class WechatMiniappAuthController : ControllerBase
{
    private readonly IWechatMiniappAuthService _wechatMiniappAuthService;

    public WechatMiniappAuthController(IWechatMiniappAuthService wechatMiniappAuthService)
    {
        _wechatMiniappAuthService = wechatMiniappAuthService;
    }

    /// <summary>
    /// 静默登录（已绑定用户返回 token，未绑定用户返回 openId）
    /// </summary>
    /// <param name="code">微信登录凭证（wx.login 获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("silent-login")]
    public async Task<Result<WechatMiniappLoginResultDto>> SilentLogin(
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        var result = await _wechatMiniappAuthService.SilentLoginAsync(code, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 手机号快捷登录（企业小程序：一步完成登录并自动创建新用户）
    /// </summary>
    /// <param name="loginCode">微信登录凭证（wx.login 获取）</param>
    /// <param name="phoneCode">手机号授权凭证（getPhoneNumber 事件获取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("phone-login")]
    public async Task<Result<AuthenticationTokenDto>> PhoneLogin(
        [FromQuery] string loginCode,
        [FromQuery] string phoneCode,
        CancellationToken cancellationToken)
    {
        var result = await _wechatMiniappAuthService.PhoneLoginAsync(loginCode, phoneCode, cancellationToken);
        return Result.Success(result);
    }

    /// <summary>
    /// 绑定手机号（用于静默登录后未绑定手机号的用户；成功后自动登录）
    /// </summary>
    /// <param name="openId">微信用户唯一标识（静默登录返回）</param>
    /// <param name="mobile">手机号码</param>
    /// <param name="smsCode">短信验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    [AllowAnonymous]
    [HttpPost("bind-mobile")]
    public async Task<Result<AuthenticationTokenDto>> BindMobile(
        [FromQuery] string openId,
        [FromQuery] string mobile,
        [FromQuery] string smsCode,
        CancellationToken cancellationToken)
    {
        var result = await _wechatMiniappAuthService.BindMobileAsync(openId, mobile, smsCode, cancellationToken);
        return Result.Success(result);
    }
}
