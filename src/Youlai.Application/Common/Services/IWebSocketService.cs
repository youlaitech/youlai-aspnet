namespace Youlai.Application.Common.Services;

/// <summary>
/// WebSocket 推送服务
/// </summary>
public interface IWebSocketService
{
    /// <summary>
    /// 广播字典变更
    /// </summary>
    Task BroadcastDictChangeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播在线人数
    /// </summary>
    Task BroadcastOnlineCountAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向指定用户推送消息
    /// </summary>
    Task SendUserMessageAsync(long userId, object payload, CancellationToken cancellationToken = default);
}
