using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using Youlai.Application.Auth;
using Youlai.Application.Auth.Models;
using Youlai.Application.Exceptions;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Domain.Entities;

using Youlai.Application.Extensions;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 扫码登录接口。
/// generate/status/login 不需要登录态（PC 端未登录），由 [AllowAnonymous] 放行；
/// scan/confirm/cancel 需要 APP 端登录态，从 ICurrentUser 取当前用户 ID。
/// </summary>
[ApiController]
[Route("api/v1/auth/qr-code")]
[Authorize]
[Tags("01.认证中心")]
/// <summary>
/// 扫码登录接口
/// </summary>
public sealed class QrCodeController : ControllerBase
{
    private const int DefaultExpireSeconds = 300;
    private const int MinRemainSeconds = 30;
    private const string QrCodeKeyFormat = "auth:qr_code:{0}";

    private const string StatusWaiting = "WAITING";
    private const string StatusScanned = "SCANNED";
    private const string StatusConfirmed = "CONFIRMED";
    private const string StatusLoggedIn = "LOGGED_IN";
    private const string StatusCanceled = "CANCELED";

    private readonly IConnectionMultiplexer _redis;
    private readonly IAuthService _authService;
    private readonly IReadOnlyDbContext _readDb;
    private readonly ICurrentUser _currentUser;

    public QrCodeController(
        IConnectionMultiplexer redis,
        IAuthService authService,
        IReadOnlyDbContext readDb,
        ICurrentUser currentUser)
    {
        _redis = redis;
        _authService = authService;
        _readDb = readDb;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("generate")]
    /// <summary>
    /// 生成扫码登录票据，返回 ticket 与有效期（秒）
    /// </summary>
    public async Task<Result<QrCodeGenerateVO>> Generate()
    {
        var db = _redis.GetDatabase();
        var ticket = Guid.NewGuid().ToString("N");
        var ctx = new QrCodeContext
        {
            Ticket = ticket,
            Status = StatusWaiting,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ClientIp = GetClientIp()
        };
        var key = string.Format(QrCodeKeyFormat, ticket);
        var json = JsonSerializer.Serialize(ctx);
        await db.StringSetAsync(key, json, TimeSpan.FromSeconds(DefaultExpireSeconds));
        return Result.Success(new QrCodeGenerateVO(ticket, DefaultExpireSeconds));
    }

    [AllowAnonymous]
    [HttpGet("status")]
    /// <summary>
    /// 查询票据当前状态（等待/已扫描/已确认/已取消）及剩余有效期
    /// </summary>
    public async Task<Result<QrCodeStatusVO>> Status([FromQuery] string ticket)
    {
        var ctx = await LoadContextAsync(ticket);
        return Result.Success(ToStatusVO(ctx, await RemainSecondsAsync(ticket)));
    }

    [HttpPost("scan")]
    /// <summary>
    /// 扫码：把当前登录用户绑定到票据，状态置为已扫描
    /// </summary>
    public async Task<Result<QrCodeStatusVO>> Scan([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ctx = await LoadContextAsync(form.Ticket);
        if (ctx.Status != StatusWaiting)
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new { u.Nickname, u.Avatar }));
        if (user is null)
            throw new BusinessException(ResultCode.AccountNotFound);
        ctx.UserId = userId;
        ctx.Nickname = user.Nickname;
        ctx.Avatar = user.Avatar;
        ctx.Status = StatusScanned;
        ctx.ScannedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await SaveContextAsync(ctx, await RefreshTtlAsync(form.Ticket));
        return Result.Success(ToStatusVO(ctx, await RemainSecondsAsync(form.Ticket)));
    }

    [HttpPost("confirm")]
    /// <summary>
    /// 确认：扫码用户确认登录，状态置为已确认
    /// </summary>
    public async Task<Result<QrCodeStatusVO>> Confirm([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ctx = await LoadContextAsync(form.Ticket);
        if (ctx.Status != StatusScanned)
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        if (ctx.UserId is null || ctx.UserId != userId)
            throw new BusinessException(ResultCode.QrCodeUserMismatch);
        ctx.Status = StatusConfirmed;
        ctx.ConfirmedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await SaveContextAsync(ctx, await RefreshTtlAsync(form.Ticket));
        return Result.Success(ToStatusVO(ctx, await RemainSecondsAsync(form.Ticket)));
    }

    [HttpPost("cancel")]
    /// <summary>
    /// 取消：终止扫码登录流程，状态置为已取消
    /// </summary>
    public async Task<Result<QrCodeStatusVO>> Cancel([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ctx = await LoadContextAsync(form.Ticket);
        if (ctx.Status != StatusWaiting && ctx.Status != StatusScanned && ctx.Status != StatusConfirmed)
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        if (ctx.Status != StatusWaiting && ctx.UserId is not null && ctx.UserId != userId)
            throw new BusinessException(ResultCode.QrCodeUserMismatch);
        ctx.Status = StatusCanceled;
        await SaveContextAsync(ctx, await RefreshTtlAsync(form.Ticket));
        return Result.Success(ToStatusVO(ctx, await RemainSecondsAsync(form.Ticket)));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    /// <summary>
    /// 登录：票据已确认时换取认证令牌
    /// </summary>
    public async Task<Result<AuthenticationTokenDto>> Login([FromBody] QrCodeTicketForm form)
    {
        var ctx = await LoadContextAsync(form.Ticket);
        if (ctx.Status != StatusConfirmed)
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        if (ctx.UserId is null)
            throw new BusinessException(ResultCode.AccountNotFound);
        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>()
                .Where(u => u.Id == ctx.UserId.Value && !u.IsDeleted)
                .Select(u => new { u.Status }));
        if (user is null)
            throw new BusinessException(ResultCode.AccountNotFound);
        if (user.Status != 1)
            throw new BusinessException(ResultCode.AccountFrozen);
        var token = await _authService.LoginByQrAsync(ctx.UserId.Value);
        // 换取令牌成功后立即把票据置为已使用（一次性），再次 login 会在状态校验处被拒，杜绝重放
        ctx.Status = StatusLoggedIn;
        var remain = await RemainSecondsAsync(form.Ticket);
        await SaveContextAsync(ctx, remain > MinRemainSeconds ? remain : MinRemainSeconds);
        return Result.Success(token);
    }

    private async Task<QrCodeContext> LoadContextAsync(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            throw new BusinessException(ResultCode.QrCodeNotFound);
        var db = _redis.GetDatabase();
        var key = string.Format(QrCodeKeyFormat, ticket);
        var raw = await db.StringGetAsync(key);
        if (!raw.HasValue)
            throw new BusinessException(ResultCode.QrCodeNotFound);
        return JsonSerializer.Deserialize<QrCodeContext>(raw.ToString())
               ?? throw new BusinessException(ResultCode.QrCodeNotFound);
    }

    private async Task SaveContextAsync(QrCodeContext ctx, int ttl)
    {
        var db = _redis.GetDatabase();
        var key = string.Format(QrCodeKeyFormat, ctx.Ticket);
        var json = JsonSerializer.Serialize(ctx);
        await db.StringSetAsync(key, json, TimeSpan.FromSeconds(ttl));
    }

    private async Task<int> RemainSecondsAsync(string ticket)
    {
        var db = _redis.GetDatabase();
        var key = string.Format(QrCodeKeyFormat, ticket);
        var ttl = await db.KeyTimeToLiveAsync(key);
        var sec = (int)(ttl?.TotalSeconds ?? 0);
        return sec > 0 ? sec : 0;
    }

    private async Task<int> RefreshTtlAsync(string ticket)
    {
        var remain = await RemainSecondsAsync(ticket);
        return remain < MinRemainSeconds ? MinRemainSeconds : remain;
    }

    private static QrCodeStatusVO ToStatusVO(QrCodeContext ctx, int expireSeconds)
    {
        string? nickname = null;
        string? avatar = null;
        if (ctx.Status is StatusScanned or StatusConfirmed)
        {
            nickname = MaskNickname(ctx.Nickname);
            avatar = ctx.Avatar;
        }
        return new QrCodeStatusVO(ctx.Ticket, ctx.Status, nickname, avatar, expireSeconds);
    }

    private static string? MaskNickname(string? nickname)
    {
        if (string.IsNullOrEmpty(nickname)) return nickname;
        var chars = nickname.ToCharArray();
        var n = chars.Length;
        if (n <= 1) return nickname;
        if (n == 2) return $"{chars[0]}*";
        return $"{chars[0]}{new string('*', n - 2)}{chars[^1]}";
    }

    private string GetClientIp()
    {
        var xff = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xff))
            return xff.Split(',')[0].Trim();
        var xrip = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xrip))
            return xrip.Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>扫码登录票据上下文，序列化为 JSON 存入 Redis。</summary>
public sealed class QrCodeContext
{
    /// <summary>票据标识</summary>
    public string Ticket { get; set; } = "";
    /// <summary>状态：waiting / scanned / confirmed / canceled</summary>
    public string Status { get; set; } = "";
    /// <summary>扫码用户 ID，未扫码为 null</summary>
    public long? UserId { get; set; }
    /// <summary>扫码用户昵称</summary>
    public string? Nickname { get; set; }
    /// <summary>扫码用户头像</summary>
    public string? Avatar { get; set; }
    /// <summary>创建时间（Unix 毫秒）</summary>
    public long? CreatedAt { get; set; }
    /// <summary>扫码时间（Unix 毫秒）</summary>
    public long? ScannedAt { get; set; }
    /// <summary>确认时间（Unix 毫秒）</summary>
    public long? ConfirmedAt { get; set; }
    /// <summary>创建票据的客户端 IP</summary>
    public string? ClientIp { get; set; }
}

/// <summary>generate 接口响应。</summary>
/// <param name="Ticket">二维码票据</param>
/// <param name="ExpireSeconds">票据有效秒数</param>
public sealed record QrCodeGenerateVO(string Ticket, int ExpireSeconds);

/// <summary>status 接口响应。</summary>
/// <param name="Ticket">二维码票据</param>
/// <param name="Status">扫码状态（待扫码/已扫码/已确认/已取消）</param>
/// <param name="Nickname">扫码用户昵称（未扫码时为空）</param>
/// <param name="Avatar">扫码用户头像（未扫码时为空）</param>
/// <param name="ExpireSeconds">票据剩余有效秒数</param>
public sealed record QrCodeStatusVO(string Ticket, string Status, string? Nickname, string? Avatar, int ExpireSeconds);

/// <summary>scan/confirm/cancel/login 请求体。</summary>
/// <param name="Ticket">二维码票据</param>
public sealed record QrCodeTicketForm(string Ticket);

