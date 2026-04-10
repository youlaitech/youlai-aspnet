using System.Threading.Channels;
using Youlai.Application.Security;

namespace Youlai.Application.Common.Interfaces;

/// <summary>
/// SSE 推送服务
/// </summary>
public interface ISseService
{
    /// <summary>
    /// 创建 SSE 连接通道
    /// </summary>
    ChannelReader<SseMessage> CreateConnection(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播字典变更
    /// </summary>
    Task BroadcastDictChangeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播在线人数
    /// </summary>
    Task BroadcastOnlineCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 向指定用户推送消息
    /// </summary>
    Task SendToUserAsync(string username, string eventName, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    IReadOnlyList<OnlineUserDto> GetOnlineUsers();

    /// <summary>
    /// 获取在线用户数
    /// </summary>
    int GetOnlineUserCount();
}
