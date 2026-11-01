namespace Youlai.Application.Realtime;

/// <summary>
/// 一次性握手票据的存储，实现需保证取出即删除
/// </summary>
public interface IRealtimeTicketStore
{
    /// <summary>保存票据，到期后自动失效</summary>
    Task SaveAsync(string ticket, RealtimeTicketGrant grant, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>取出并删除票据，票据不存在或已使用返回 null</summary>
    Task<RealtimeTicketGrant?> TakeAsync(string ticket, CancellationToken cancellationToken = default);
}
