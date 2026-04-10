using System.Net.WebSockets;
using System.Text;
using Youlai.Infrastructure.WebSockets;
using Xunit;

namespace Youlai.Api.Tests;

public sealed class StompBrokerTests
{
    [Fact]
    public async Task BroadcastDict_ShouldSendMessageToTopicSubscribers()
    {
        var broker = new StompBroker();
        var ws = new FakeWebSocket();
        var conn = new StompConnection("c1", ws, userId: 1);
        broker.RegisterConnection(conn);

        broker.Subscribe(conn, "sub-1", "/topic/dict");

        await broker.BroadcastAsync("/topic/dict", new { dictCode = "gender", timestamp = 1L });

        var payload = ws.GetAllText();
        Assert.Contains("MESSAGE\n", payload);
        Assert.Contains("destination:/topic/dict", payload);
        Assert.Contains("\"dictCode\":\"gender\"", payload);
    }

    [Fact]
    public async Task SendToUser_ShouldSendMessageToUserQueueSubscribers()
    {
        var broker = new StompBroker();
        var ws = new FakeWebSocket();
        var conn = new StompConnection("c1", ws, userId: 1);
        broker.RegisterConnection(conn);

        broker.Subscribe(conn, "sub-1", "/user/queue/message");

        await broker.SendToUserAsync(1, "/queue/message", new { id = 100, title = "hello" });

        var payload = ws.GetAllText();
        Assert.Contains("MESSAGE\n", payload);
        Assert.Contains("destination:/user/queue/message", payload);
        Assert.Contains("\"id\":100", payload);
        Assert.Contains("\"title\":\"hello\"", payload);
    }

    private sealed class FakeWebSocket : WebSocket
    {
        private readonly StringBuilder _sb = new();

        public string GetAllText() => _sb.ToString();

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (messageType == WebSocketMessageType.Text)
            {
                _sb.Append(Encoding.UTF8.GetString(buffer));
            }
            return Task.CompletedTask;
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        }

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken = default)
        {
            if (messageType == WebSocketMessageType.Text)
            {
                _sb.Append(Encoding.UTF8.GetString(buffer.Span));
            }
            return ValueTask.CompletedTask;
        }
    }
}
