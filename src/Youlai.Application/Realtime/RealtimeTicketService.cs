using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Youlai.Application.Security;

namespace Youlai.Application.Realtime;

/// <summary>
/// 签发并消费一次性握手票据，票据仅在服务端按摘要暂存
/// </summary>
internal sealed class RealtimeTicketService : IRealtimeTicketService
{
    // 票据有效期较短，取到后需尽快完成握手
    private static readonly TimeSpan TicketTtl = TimeSpan.FromSeconds(30);
    private const string TicketAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
    private const int TicketLength = 43;

    private readonly IRealtimeTicketStore _store;
    private readonly ITokenManager _tokens;
    private readonly ILogger<RealtimeTicketService> _logger;

    public RealtimeTicketService(IRealtimeTicketStore store, ITokenManager tokens, ILogger<RealtimeTicketService> logger)
    {
        _store = store;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>
    /// 校验访问凭证与订阅主题后签发票据
    /// </summary>
    public async Task<RealtimeTicketResponse?> CreateAsync(long userId, string accessToken, CreateRealtimeTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || !_tokens.ValidateAccessToken(accessToken))
        {
            return null;
        }

        var topics = new HashSet<string>(request.Topics, StringComparer.Ordinal);
        if (!topics.Contains(RealtimeTopics.Notice) || !RealtimeTopics.Supported.IsSupersetOf(topics))
        {
            _logger.LogWarning("[Realtime] 用户 {UserId} 申请票据时订阅主题不合法", userId);
            return null;
        }

        var ticket = GenerateTicket();
        var grant = new RealtimeTicketGrant
        {
            AccessToken = accessToken,
            UserId = userId,
            ClientInstanceId = string.IsNullOrWhiteSpace(request.ClientInstanceId) ? Guid.NewGuid().ToString("N") : request.ClientInstanceId,
            Platform = string.IsNullOrWhiteSpace(request.Platform) ? "web" : request.Platform,
            Topics = topics,
            ExpiresAtMillis = DateTimeOffset.UtcNow.Add(TicketTtl).ToUnixTimeMilliseconds(),
        };

        await _store.SaveAsync(ticket, grant, TicketTtl, cancellationToken).ConfigureAwait(false);
        return new RealtimeTicketResponse { Ticket = ticket };
    }

    /// <summary>
    /// 取出并删除票据后复核到期与访问凭证，已取出的票据不回滚
    /// </summary>
    public async Task<RealtimeTicketGrant?> ConsumeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        if (!IsWellFormed(ticket))
        {
            return null;
        }

        var grant = await _store.TakeAsync(ticket, cancellationToken).ConfigureAwait(false);
        if (grant is null)
        {
            return null;
        }
        if (grant.ExpiresAtMillis <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
            return null;
        }
        if (!_tokens.ValidateAccessToken(grant.AccessToken))
        {
            _logger.LogInformation("[Realtime] 用户 {UserId} 的票据对应凭证已失效", grant.UserId);
            return null;
        }
        return grant;
    }

    /// <summary>
    /// 生成 URL 安全的一次性票据
    /// </summary>
    private static string GenerateTicket()
    {
        var chars = new char[TicketLength];
        var buffer = new byte[TicketLength];
        RandomNumberGenerator.Fill(buffer);
        for (var i = 0; i < TicketLength; i++)
        {
            chars[i] = TicketAlphabet[buffer[i] % TicketAlphabet.Length];
        }
        return new string(chars);
    }

    /// <summary>
    /// 校验票据长度与字符集，避免无效串进入存储查询
    /// </summary>
    private static bool IsWellFormed(string? ticket)
    {
        if (string.IsNullOrEmpty(ticket) || ticket.Length is < 43 or > 256)
        {
            return false;
        }
        return ticket.All(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-');
    }
}
