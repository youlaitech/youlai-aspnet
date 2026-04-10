namespace Youlai.Core.Services;

/// <summary>
/// SSE 消息
/// </summary>
public sealed class SseMessage
{
    public string EventName { get; init; } = string.Empty;

    public object? Data { get; init; }
}
