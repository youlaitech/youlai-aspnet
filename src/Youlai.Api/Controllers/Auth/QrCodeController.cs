using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using Youlai.Api.Security;
using Youlai.Application.Auth;
using Youlai.Application.Auth.Models;
using Youlai.Application.Exceptions;
using Youlai.Application.Extensions;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Domain.Entities;

namespace Youlai.Api.Controllers.Auth;

/// <summary>
/// 扫码登录接口：generate/status/login 免登录，scan/confirm/cancel 需 APP 端登录态
/// </summary>
[ApiController]
[Route("api/v1/auth/qr-code")]
[Authorize]
[Tags("01.认证中心")]
public sealed class QrCodeController : ControllerBase
{
    private const int DefaultExpireSeconds = 300;
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

    /// <summary>
    /// 生成扫码登录票据，返回 ticket 与有效期（秒）
    /// </summary>
    [AllowAnonymous]
    [HttpPost("generate")]
    public async Task<Result<QrCodeGenerateVO>> Generate()
    {
        var db = _redis.GetDatabase();
        var ticket = Guid.NewGuid().ToString("N");
        var ctx = new QrCodeContext
        {
            Ticket = ticket,
            Status = StatusWaiting,
            CreatedAt = Now(),
            ClientIp = ClientIp.Resolve(HttpContext)
        };
        var key = string.Format(QrCodeKeyFormat, ticket);
        await db.StringSetAsync(key, JsonSerializer.Serialize(ctx), TimeSpan.FromSeconds(DefaultExpireSeconds));
        return Result.Success(new QrCodeGenerateVO(ticket, DefaultExpireSeconds));
    }

    /// <summary>
    /// 查询票据当前状态（等待/已扫描/已确认/已取消）及剩余有效期
    /// </summary>
    [AllowAnonymous]
    [HttpGet("status")]
    public async Task<Result<QrCodeStatusVO>> Status([FromQuery] string ticket)
    {
        var (ctx, _, ttl) = await LoadAsync(ticket);
        return Result.Success(ToStatusVO(ctx, (int)ttl.TotalSeconds));
    }

    /// <summary>
    /// 扫码：把当前登录用户绑定到票据，状态置为已扫描
    /// </summary>
    [HttpPost("scan")]
    public async Task<Result<QrCodeStatusVO>> Scan([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var (ctx, raw, ttl) = await LoadAsync(form.Ticket);
        if (ctx.Status != StatusWaiting)
        {
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        }

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new { u.Nickname, u.Avatar }));
        if (user is null)
        {
            throw new BusinessException(ResultCode.AccountNotFound);
        }

        ctx.UserId = userId;
        ctx.Nickname = user.Nickname;
        ctx.Avatar = user.Avatar;
        ctx.Status = StatusScanned;
        ctx.ScannedAt = Now();
        await SaveAsync(form.Ticket, raw, ctx, ttl);
        return Result.Success(ToStatusVO(ctx, (int)ttl.TotalSeconds));
    }

    /// <summary>
    /// 确认：扫码用户确认登录，状态置为已确认
    /// </summary>
    [HttpPost("confirm")]
    public async Task<Result<QrCodeStatusVO>> Confirm([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var (ctx, raw, ttl) = await LoadAsync(form.Ticket);
        if (ctx.Status != StatusScanned)
        {
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        }
        if (ctx.UserId is null || ctx.UserId != userId)
        {
            throw new BusinessException(ResultCode.QrCodeUserMismatch);
        }

        ctx.Status = StatusConfirmed;
        ctx.ConfirmedAt = Now();
        await SaveAsync(form.Ticket, raw, ctx, ttl);
        return Result.Success(ToStatusVO(ctx, (int)ttl.TotalSeconds));
    }

    /// <summary>
    /// 取消：终止扫码登录流程，状态置为已取消
    /// </summary>
    [HttpPost("cancel")]
    public async Task<Result<QrCodeStatusVO>> Cancel([FromBody] QrCodeTicketForm form)
    {
        var userId = _currentUser.GetRequiredUserId();
        var (ctx, raw, ttl) = await LoadAsync(form.Ticket);
        if (ctx.Status != StatusWaiting && ctx.Status != StatusScanned && ctx.Status != StatusConfirmed)
        {
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        }
        if (ctx.Status != StatusWaiting && ctx.UserId is not null && ctx.UserId != userId)
        {
            throw new BusinessException(ResultCode.QrCodeUserMismatch);
        }

        ctx.Status = StatusCanceled;
        await SaveAsync(form.Ticket, raw, ctx, ttl);
        return Result.Success(ToStatusVO(ctx, (int)ttl.TotalSeconds));
    }

    /// <summary>
    /// 登录：票据已确认时换取认证令牌
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<Result<AuthenticationTokenDto>> Login([FromBody] QrCodeTicketForm form)
    {
        var (ctx, raw, ttl) = await LoadAsync(form.Ticket);
        if (ctx.Status != StatusConfirmed)
        {
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        }
        if (ctx.UserId is null)
        {
            throw new BusinessException(ResultCode.AccountNotFound);
        }

        var user = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysUser>()
                .Where(u => u.Id == ctx.UserId.Value && !u.IsDeleted)
                .Select(u => new { u.Status }));
        if (user is null)
        {
            throw new BusinessException(ResultCode.AccountNotFound);
        }
        if (user.Status != 1)
        {
            throw new BusinessException(ResultCode.AccountFrozen);
        }

        // 先原子消费票据再签发令牌，并发请求只有一个能换到令牌
        ctx.Status = StatusLoggedIn;
        await SaveAsync(form.Ticket, raw, ctx, ttl);

        var token = await _authService.LoginByQrAsync(ctx.UserId.Value);
        return Result.Success(token);
    }

    /// <summary>
    /// 读取票据上下文与剩余有效期，票据不存在或已过期时报错
    /// </summary>
    private async Task<(QrCodeContext Context, string Raw, TimeSpan Ttl)> LoadAsync(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            throw new BusinessException(ResultCode.QrCodeNotFound);
        }

        var db = _redis.GetDatabase();
        var key = string.Format(QrCodeKeyFormat, ticket);
        var value = await db.StringGetWithExpiryAsync(key);
        if (!value.Value.HasValue || value.Expiry is null || value.Expiry <= TimeSpan.Zero)
        {
            throw new BusinessException(ResultCode.QrCodeNotFound);
        }

        var raw = value.Value.ToString();
        var ctx = JsonSerializer.Deserialize<QrCodeContext>(raw)
                  ?? throw new BusinessException(ResultCode.QrCodeNotFound);
        return (ctx, raw, value.Expiry.Value);
    }

    /// <summary>
    /// 以读取时的值为条件写回，期间被其他请求修改时拒绝本次更新
    /// </summary>
    private async Task SaveAsync(string ticket, string raw, QrCodeContext ctx, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        var key = string.Format(QrCodeKeyFormat, ticket);
        var transaction = db.CreateTransaction();
        transaction.AddCondition(Condition.StringEqual(key, raw));
        _ = transaction.StringSetAsync(key, JsonSerializer.Serialize(ctx), ttl);
        if (!await transaction.ExecuteAsync())
        {
            throw new BusinessException(ResultCode.QrCodeStatusIllegal);
        }
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
}

/// <summary>扫码登录票据上下文，序列化为 JSON 存入 Redis</summary>
public sealed class QrCodeContext
{
    /// <summary>票据标识</summary>
    public string Ticket { get; set; } = "";
    /// <summary>状态：WAITING / SCANNED / CONFIRMED / LOGGED_IN / CANCELED</summary>
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

/// <summary>generate 接口响应</summary>
/// <param name="Ticket">二维码票据</param>
/// <param name="ExpireSeconds">票据有效秒数</param>
public sealed record QrCodeGenerateVO(string Ticket, int ExpireSeconds);

/// <summary>status 接口响应</summary>
/// <param name="Ticket">二维码票据</param>
/// <param name="Status">扫码状态（待扫码/已扫码/已确认/已取消）</param>
/// <param name="Nickname">扫码用户昵称（未扫码时为空）</param>
/// <param name="Avatar">扫码用户头像（未扫码时为空）</param>
/// <param name="ExpireSeconds">票据剩余有效秒数</param>
public sealed record QrCodeStatusVO(string Ticket, string Status, string? Nickname, string? Avatar, int ExpireSeconds);

/// <summary>scan/confirm/cancel/login 请求体</summary>
/// <param name="Ticket">二维码票据</param>
public sealed record QrCodeTicketForm(string Ticket);
