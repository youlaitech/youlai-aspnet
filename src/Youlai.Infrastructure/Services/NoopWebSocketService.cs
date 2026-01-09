using Microsoft.Extensions.Logging;
using Youlai.Application.Common.Services;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// WebSocket 空实现
/// </summary>
internal sealed class NoopWebSocketService : IWebSocketService
{
    private readonly ILogger<NoopWebSocketService> _logger;

    public NoopWebSocketService(ILogger<NoopWebSocketService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 广播字典变更
    /// </summary>
    public Task BroadcastDictChangeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[WebSocket] BroadcastDictChange dictCode={DictCode}", dictCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 广播在线人数
    /// </summary>
    public Task BroadcastOnlineCountAsync(int count, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[WebSocket] BroadcastOnlineCount count={Count}", count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 向指定用户推送消息
    /// </summary>
    public Task SendUserMessageAsync(long userId, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[WebSocket] SendUserMessage userId={UserId} payloadType={PayloadType}", userId, payload?.GetType().Name);
        return Task.CompletedTask;
    }
}
