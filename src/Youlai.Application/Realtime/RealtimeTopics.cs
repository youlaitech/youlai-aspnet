namespace Youlai.Application.Realtime;

/// <summary>
/// 实时订阅主题
/// </summary>
public static class RealtimeTopics
{
    /// <summary>公告推送，连接必须订阅</summary>
    public const string Notice = "notice";

    /// <summary>字典变更</summary>
    public const string Dict = "dict";

    /// <summary>在线状态</summary>
    public const string Presence = "presence";

    /// <summary>当前支持的全部主题</summary>
    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        Notice, Dict, Presence,
    };

    /// <summary>连接就绪，携带心跳超时时间</summary>
    public const string EventConnectionReady = "connection.ready";

    /// <summary>服务端心跳</summary>
    public const string EventHeartbeatPing = "heartbeat.ping";

    /// <summary>公告变更</summary>
    public const string EventNoticeChanged = "notice.changed";

    /// <summary>字典变更</summary>
    public const string EventDictChanged = "dict.changed";

    /// <summary>在线状态快照</summary>
    public const string EventPresenceSnapshot = "presence.snapshot";

    /// <summary>心跳超时时间（毫秒），与前端协议保持一致</summary>
    public const int HeartbeatTimeoutMs = 60_000;
}
