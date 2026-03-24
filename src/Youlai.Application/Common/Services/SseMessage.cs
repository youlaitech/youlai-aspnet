namespace Youlai.Application.Common.Services;

/// <summary>
/// SSE 消息
/// </summary>
public sealed class SseMessage
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// 消息数据
    /// </summary>
    public object? Data { get; init; }
}