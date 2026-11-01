namespace Youlai.Application.Realtime;

/// <summary>
/// 票据承载的握手授权内容，仅在服务端流转，不得写入响应或日志
/// </summary>
public sealed record RealtimeTicketGrant
{
    /// <summary>换取票据时使用的访问令牌，握手时复核是否仍然有效</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>授权用户</summary>
    public long UserId { get; init; }

    /// <summary>客户端实例标识</summary>
    public string ClientInstanceId { get; init; } = string.Empty;

    /// <summary>运行平台</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>本次连接固定的订阅主题</summary>
    public HashSet<string> Topics { get; init; } = new(StringComparer.Ordinal);

    /// <summary>到期时间（Unix 毫秒）</summary>
    public long ExpiresAtMillis { get; init; }
}
