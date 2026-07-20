using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Youlai.Application.Auth.Models;
using Youlai.Application.Auth;
using Youlai.Application.Attributes;
using Youlai.Domain.Enums;
using Youlai.Application.Results;
using Youlai.Application.Common;
using Youlai.Api.Filters;
using Microsoft.AspNetCore.Http;

namespace Youlai.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
[Authorize]
[Tags("01.认证中心")]
/// <summary>
/// 认证接口（登录、刷新令牌、退出、验证码、短信登录）
/// </summary>
public sealed class AuthController : ControllerBase
{
    private readonly ICaptchaService _captchaService;
    private readonly IAuthService _authService;
    private readonly ILoggingService _loggingService;
    private readonly IDistributedCache _cache;

    public AuthController(ICaptchaService captchaService, IAuthService authService, ILoggingService loggingService, IDistributedCache cache)
    {
        _captchaService = captchaService;
        _authService = authService;
        _loggingService = loggingService;
        _cache = cache;
    }

    /// <summary>
    /// 获取验证码
    /// </summary>
    [AllowAnonymous]
    [HttpGet("captcha")]
    public async Task<Result<CaptchaInfoDto>> GetCaptcha(CancellationToken cancellationToken)
    {
        var captcha = await _captchaService.GenerateAsync(cancellationToken);
        return Result.Success(captcha);
    }

    /// <summary>
    /// 登录
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<Result<AuthenticationTokenDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var (token, userId) = await _authService.LoginAsync(request, cancellationToken);

        if (userId > 0)
        {
            await _loggingService.RecordLoginLogAsync(userId, "/api/v1/auth/login", cancellationToken);
        }

        return Result.Success(token);
    }

    /// <summary>
    /// 发送登录短信验证码
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sms/code")]
    public async Task<IActionResult> SendSmsLoginCode([FromQuery] string mobile, CancellationToken cancellationToken)
    {
        // 同一手机号 60 秒内仅允许发送一次验证码
        var smsKey = $"rate_limit:api:sms:{mobile}";
        if (!string.IsNullOrWhiteSpace(await _cache.GetStringAsync(smsKey, cancellationToken)))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                Result.Failed(ResultCode.RequestConcurrencyLimitExceeded));
        }

        await _cache.SetStringAsync(smsKey, "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
            cancellationToken);

        await _authService.SendSmsLoginCodeAsync(mobile, cancellationToken);
        return Ok(Result.Success());
    }

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login/sms")]
    public async Task<Result<AuthenticationTokenDto>> LoginBySms([FromQuery] string mobile, [FromQuery] string code, CancellationToken cancellationToken)
    {
        var (token, userId) = await _authService.LoginBySmsAsync(mobile, code, cancellationToken);

        if (userId > 0)
        {
            await _loggingService.RecordLoginLogAsync(userId, "/api/v1/auth/login/sms", cancellationToken);
        }

        return Result.Success(token);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<Result<AuthenticationTokenDto>> RefreshToken([FromQuery] string refreshToken, CancellationToken cancellationToken)
    {
        var token = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        return Result.Success(token);
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [AllowAnonymous]
    [HttpDelete("logout")]
    [Log(LogModule.LOGIN, ActionType.LOGOUT)]
    public async Task<Result<object?>> Logout(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        await _authService.LogoutAsync(authorization, cancellationToken);
        return Result.Success();
    }
}

