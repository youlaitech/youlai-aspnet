namespace Youlai.Application.Common;

/// <summary>
/// SSE 消息
/// </summary>
public sealed record SseMessage
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// 数据载荷
    /// </summary>
    public object? Data { get; init; }
}
