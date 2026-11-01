namespace Youlai.Application.Realtime;

/// <summary>
/// 签发的握手票据，只能使用一次且 30 秒内有效
/// </summary>
public sealed record RealtimeTicketResponse
{
    /// <summary>握手时通过查询参数提交的票据</summary>
    public string Ticket { get; init; } = string.Empty;
}
