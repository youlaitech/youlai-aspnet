using Youlai.Application.Common.Services;
using Youlai.Infrastructure.WebSockets;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// WebSocket 服务实现（基于 STOMP Broker）
/// </summary>
/// <remarks>
/// 对外提供字典变更、在线人数与个人消息推送能力
/// </remarks>
internal sealed class StompWebSocketService : IWebSocketService
{
    private readonly StompBroker _broker;

    public StompWebSocketService(StompBroker broker)
    {
        _broker = broker;
    }

    /// <summary>
    /// 广播字典变更
    /// </summary>
    public Task BroadcastDictChangeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dictCode))
        {
            return Task.CompletedTask;
        }

        var payload = new
        {
            dictCode,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return _broker.BroadcastAsync("/topic/dict", payload, cancellationToken);
    }

    /// <summary>
    /// 广播在线人数
    /// </summary>
    public Task BroadcastOnlineCountAsync(int count, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            count,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return _broker.BroadcastAsync("/topic/online-count", payload, cancellationToken);
    }

    /// <summary>
    /// 向指定用户推送消息
    /// </summary>
    public Task SendUserMessageAsync(long userId, object payload, CancellationToken cancellationToken = default)
    {
        return _broker.SendToUserAsync(userId, "/queue/message", payload, cancellationToken);
    }
}
