namespace Youlai.Application.Realtime;

/// <summary>
/// 签发并消费实时连接的一次性握手票据
/// </summary>
public interface IRealtimeTicketService
{
    /// <summary>
    /// 校验访问凭证后签发票据，凭证无效或订阅主题不合法返回 null
    /// </summary>
    Task<RealtimeTicketResponse?> CreateAsync(long userId, string accessToken, CreateRealtimeTicketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取出并删除票据后复核凭证，票据无效、已使用或已过期返回 null
    /// </summary>
    Task<RealtimeTicketGrant?> ConsumeAsync(string ticket, CancellationToken cancellationToken = default);
}
