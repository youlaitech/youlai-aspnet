using Youlai.Application.System.Models.User;

namespace Youlai.Application.Realtime;

/// <summary>
/// 实时连接会话管理，负责连接注册、主题广播与在线状态
/// </summary>
public interface IRealtimeService
{
    /// <summary>
    /// 注册连接，同一客户端实例重连会替换旧连接；连接数超出上限返回 false
    /// </summary>
    bool TryRegister(IRealtimeClient client);

    /// <summary>
    /// 移除连接并释放配额
    /// </summary>
    void Unregister(IRealtimeClient client);

    /// <summary>
    /// 向订阅了该主题的全部连接广播消息
    /// </summary>
    Task BroadcastAsync(string topic, string type, object? payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向指定用户的全部连接推送消息
    /// </summary>
    Task SendToUserAsync(long userId, string type, object? payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取在线用户列表，登录时间取该用户最早的连接时间
    /// </summary>
    IReadOnlyList<OnlineUserDto> GetOnlineUsers();

    /// <summary>
    /// 在线用户数，按用户去重（一个用户多标签页算一人）
    /// </summary>
    int GetOnlineUserCount();

    /// <summary>
    /// 生成在线状态快照
    /// </summary>
    RealtimePresenceSnapshot PresenceSnapshot();

    /// <summary>
    /// 广播在线状态快照
    /// </summary>
    void BroadcastPresence();

    /// <summary>
    /// 关闭全部连接，用于应用停机
    /// </summary>
    Task CloseAllAsync(CancellationToken cancellationToken = default);
}
