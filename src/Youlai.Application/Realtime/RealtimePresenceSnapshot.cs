namespace Youlai.Application.Realtime;

/// <summary>
/// 当前节点的在线状态快照
/// </summary>
public sealed record RealtimePresenceSnapshot
{
    /// <summary>在线用户数，按用户去重</summary>
    public int OnlineUsers { get; init; }

    /// <summary>在线连接数，多标签页会重复计数</summary>
    public int OnlineConnections { get; init; }

    /// <summary>节点标识</summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>观测时间，UTC 毫秒精度</summary>
    public string ObservedAt { get; init; } = string.Empty;
}
