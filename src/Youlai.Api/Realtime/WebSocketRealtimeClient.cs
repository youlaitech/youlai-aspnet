using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Youlai.Application.Realtime;

namespace Youlai.Api.Realtime;

/// <summary>
/// 用 WebSocket 承载的实时连接
/// </summary>
internal sealed class WebSocketRealtimeClient : IRealtimeClient
{
    // 与 youlai-boot 的单帧上限保持一致
    private const int MaxMessageBytes = 16_384;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WebSocket _socket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocketRealtimeClient(
        string id,
        long userId,
        string clientInstanceId,
        string platform,
        IReadOnlySet<string> topics,
        WebSocket socket)
    {
        Id = id;
        UserId = userId;
        ClientInstanceId = clientInstanceId;
        Platform = platform;
        Topics = topics;
        ConnectedAt = DateTimeOffset.UtcNow;
        _socket = socket;
    }

    public string Id { get; }

    public long UserId { get; }

    public string ClientInstanceId { get; }

    public string Platform { get; }

    public IReadOnlySet<string> Topics { get; }

    public DateTimeOffset ConnectedAt { get; }

    /// <summary>
    /// 发送消息，WebSocket 不支持并发写入，发送串行化
    /// </summary>
    public async Task SendAsync(RealtimeMessage message, CancellationToken cancellationToken = default)
    {
        if (_socket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("连接已关闭");
        }

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > MaxMessageBytes)
        {
            throw new InvalidOperationException("消息超出单帧上限");
        }

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 关闭连接，已关闭的连接不再重复关闭
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", cancellationToken).ConfigureAwait(false);
        }
    }
}
