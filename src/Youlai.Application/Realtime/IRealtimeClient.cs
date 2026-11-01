namespace Youlai.Application.Realtime;

/// <summary>
/// 一条实时连接，由传输层（WebSocket）实现后交给会话管理器托管
/// </summary>
public interface IRealtimeClient
{
    /// <summary>连接标识</summary>
    string Id { get; }

    /// <summary>所属用户</summary>
    long UserId { get; }

    /// <summary>客户端实例标识，同一实例重连时用于替换旧连接</summary>
    string ClientInstanceId { get; }

    /// <summary>运行平台，如 web</summary>
    string Platform { get; }

    /// <summary>本次连接订阅的主题</summary>
    IReadOnlySet<string> Topics { get; }

    /// <summary>连接建立时间</summary>
    DateTimeOffset ConnectedAt { get; }

    /// <summary>向该连接发送消息</summary>
    Task SendAsync(RealtimeMessage message, CancellationToken cancellationToken = default);

    /// <summary>关闭该连接</summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
