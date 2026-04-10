namespace Youlai.Application.Security;

/// <summary>
/// SSE message
/// </summary>
public sealed record SseMessage
{
    /// <summary>
    /// Event name
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// Data payload
    /// </summary>
    public object? Data { get; init; }
}
