namespace Youlai.Application.Realtime;

/// <summary>
/// 实时消息，与 youlai-boot 的协议一致
/// </summary>
public sealed record RealtimeMessage
{
    /// <summary>协议版本，当前固定为 1</summary>
    public int Version { get; init; } = 1;

    /// <summary>消息唯一标识，用于客户端去重</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>消息类型，如 notice、dict、presence</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>消息载荷</summary>
    public object? Payload { get; init; }

    /// <summary>
    /// 创建一条服务端消息并生成唯一标识
    /// </summary>
    public static RealtimeMessage Create(string type, object? payload)
    {
        return new RealtimeMessage
        {
            Version = 1,
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Payload = payload,
        };
    }
}
