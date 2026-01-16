using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Results;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 璁よ瘉鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鐧诲綍銆佸埛鏂颁护鐗屻€侀€€鍑虹櫥褰曠瓑鑳藉姏
/// </remarks>
[ApiController]
[Route("api/v1/auth")]
[Authorize]
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
    /// 鑾峰彇楠岃瘉鐮?
    /// </summary>
    [AllowAnonymous]
    [HttpGet("captcha")]
    public async Task<Result<CaptchaInfoDto>> GetCaptcha(CancellationToken cancellationToken)
    {
        var captcha = await _captchaService.GenerateAsync(cancellationToken);
        return Result.Success(captcha);
    }

    /// <summary>
    /// 鐧诲綍
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<Result<AuthenticationTokenDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request, cancellationToken);
        return Result.Success(token);
    }

    /// <summary>
    /// 鍒锋柊浠ょ墝
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<Result<AuthenticationTokenDto>> RefreshToken([FromQuery] string refreshToken, CancellationToken cancellationToken)
    {
        var token = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        return Result.Success(token);
    }

    /// <summary>
    /// 閫€鍑虹櫥褰?
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
