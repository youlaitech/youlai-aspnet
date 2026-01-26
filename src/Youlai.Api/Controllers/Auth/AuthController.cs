using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 认证接口
/// </summary>
/// <remarks>
/// 提供登录、刷新令牌、退出登录等能力。
/// </remarks>
[ApiController]
[Route("api/v1/auth")]
[Authorize]
[Tags("01.认证接口")]
public sealed class AuthController : ControllerBase
{
    private readonly ICaptchaService _captchaService;
    private readonly IAuthService _authService;

    public AuthController(ICaptchaService captchaService, IAuthService authService)
    {
        _captchaService = captchaService;
        _authService = authService;
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
        var token = await _authService.LoginAsync(request, cancellationToken);
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
    public async Task<Result<object?>> Logout(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        await _authService.LogoutAsync(authorization, cancellationToken);
        return Result.Success();
    }
}
