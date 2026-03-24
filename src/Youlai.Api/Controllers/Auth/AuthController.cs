using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Auth.Dtos;
using Youlai.Application.Auth.Services;
using Youlai.Application.Common.Attributes;
using Youlai.Application.Common.Enums;
using Youlai.Infrastructure.Common.Filters;
using Youlai.Application.Common.Results;
using Youlai.Domain.Entities;
using Youlai.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

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
[Tags("01.认证中心")]
public sealed class AuthController : ControllerBase
{
    private readonly ICaptchaService _captchaService;
    private readonly IAuthService _authService;
    private readonly YoulaiDbContext _dbContext;

    public AuthController(ICaptchaService captchaService, IAuthService authService, YoulaiDbContext dbContext)
    {
        _captchaService = captchaService;
        _authService = authService;
        _dbContext = dbContext;
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

        // 手动记录登录日志
        var username = request.Username.Trim();
        var userId = await _dbContext.SysUsers.AsNoTracking()
            .Where(u => u.Username == username && !u.IsDeleted)
            .Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
        if (userId > 0)
        {
            await RecordLoginLogAsync(userId, "/api/v1/auth/login", cancellationToken);
        }

        return Result.Success(token);
    }

    /// <summary>
    /// 发送登录短信验证码
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sms/code")]
    public async Task<Result<object?>> SendSmsLoginCode([FromQuery] string mobile, CancellationToken cancellationToken)
    {
        await _authService.SendSmsLoginCodeAsync(mobile, cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login/sms")]
    public async Task<Result<AuthenticationTokenDto>> LoginBySms([FromQuery] string mobile, [FromQuery] string code, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginBySmsAsync(mobile, code, cancellationToken);

        // 手动记录登录日志
        var userId = await _dbContext.SysUsers.AsNoTracking()
            .Where(u => u.Mobile == mobile.Trim() && !u.IsDeleted)
            .Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
        if (userId > 0)
        {
            await RecordLoginLogAsync(userId, "/api/v1/auth/login/sms", cancellationToken);
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

    /// <summary>
    /// 手动记录登录日志（登录接口为 AllowAnonymous，LogActionFilter 无法获取用户信息）
    /// </summary>
    private async Task RecordLoginLogAsync(long userId, string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var (browser, os) = LogActionFilter.ParseUserAgent(userAgent);

            var log = new SysLog
            {
                Module = (int)LogModule.LOGIN,
                ActionType = (int)ActionType.LOGIN,
                RequestUri = requestUri,
                RequestMethod = "POST",
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Browser = browser,
                Os = os,
                Status = 1,
                OperatorId = userId,
                CreateTime = DateTime.Now,
            };

            _dbContext.SysLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // 日志记录失败不影响登录
        }
    }
}
