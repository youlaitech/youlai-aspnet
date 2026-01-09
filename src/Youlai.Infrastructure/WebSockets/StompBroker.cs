using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Youlai.Infrastructure.WebSockets;

/// <summary>
/// STOMP Broker（内存实现），用于管理连接/订阅与消息分发
/// </summary>
/// <remarks>
/// 用于在服务端维护订阅关系，并向 topic 或 user-queue 分发消息
/// </remarks>
public sealed class StompBroker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, StompConnection>> _subscriptions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, StompConnection>> _userConnections = new();
    private readonly ConcurrentDictionary<string, StompConnection> _connections = new(StringComparer.Ordinal);

    public int ConnectedCount => _connections.Count;

    public int OnlineUserCount => _userConnections.Count;

    public void RegisterConnection(StompConnection connection)
    {
        _connections[connection.ConnectionId] = connection;

        if (connection.UserId.HasValue && connection.UserId.Value > 0)
        {
            var map = _userConnections.GetOrAdd(connection.UserId.Value, _ => new ConcurrentDictionary<string, StompConnection>(StringComparer.Ordinal));
            map[connection.ConnectionId] = connection;
        }
    }

    public void UnregisterConnection(StompConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);

        foreach (var kv in connection.SubscriptionsById)
        {
            Unsubscribe(connection, kv.Key);
        }

        if (connection.UserId.HasValue && connection.UserId.Value > 0)
        {
            if (_userConnections.TryGetValue(connection.UserId.Value, out var map))
            {
                map.TryRemove(connection.ConnectionId, out _);
                if (map.IsEmpty)
                {
                    _userConnections.TryRemove(connection.UserId.Value, out _);
                }
            }
        }
    }

    public void Subscribe(StompConnection connection, string destination)
    {
        Subscribe(connection, subscriptionId: Guid.NewGuid().ToString("N"), destination);
    }

    public void Subscribe(StompConnection connection, string subscriptionId, string destination)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        connection.SubscriptionsById[subscriptionId] = destination;
        connection.SubscriptionIdsByDestination[destination] = subscriptionId;

        var destMap = _subscriptions.GetOrAdd(destination, _ => new ConcurrentDictionary<string, StompConnection>(StringComparer.Ordinal));
        destMap[connection.ConnectionId] = connection;
    }

    public void Unsubscribe(StompConnection connection, string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return;
        }

        if (!connection.SubscriptionsById.TryRemove(subscriptionId, out var destination) || string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        connection.SubscriptionIdsByDestination.TryRemove(destination, out _);

        if (_subscriptions.TryGetValue(destination, out var destMap))
        {
            destMap.TryRemove(connection.ConnectionId, out _);
            if (destMap.IsEmpty)
            {
                _subscriptions.TryRemove(destination, out _);
            }
        }
    }

    public Task BroadcastAsync(string destination, object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return Task.CompletedTask;
        }

        if (!_subscriptions.TryGetValue(destination, out var subs) || subs.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var json = SerializePayload(payload);
        var tasks = subs.Values.Select(c => c.SendMessageAsync(destination, json, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public Task SendToUserAsync(long userId, string userDestination, object payload, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(userDestination))
        {
            return Task.CompletedTask;
        }

        if (!_userConnections.TryGetValue(userId, out var conns) || conns.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var json = SerializePayload(payload);

        var effectiveDestination = userDestination.StartsWith("/user/", StringComparison.Ordinal)
            ? userDestination
            : $"/user{userDestination}";

        var tasks = conns.Values
            .Where(c => c.SubscriptionIdsByDestination.ContainsKey(effectiveDestination))
            .Select(c => c.SendMessageAsync(effectiveDestination, json, cancellationToken));

        return Task.WhenAll(tasks);
    }

    private static string SerializePayload(object payload)
    {
        if (payload is string s)
        {
            return s;
        }

        return JsonSerializer.Serialize(payload);
    }
}

/// <summary>
/// STOMP 连接会话（绑定到单个 WebSocket）
/// </summary>
/// <remarks>
/// 记录连接信息与订阅关系，并提供发送 STOMP 帧的能力
/// </remarks>
public sealed class StompConnection
{
    public StompConnection(string connectionId, WebSocket webSocket, long? userId)
    {
        ConnectionId = connectionId;
        WebSocket = webSocket;
        UserId = userId;
    }

    public string ConnectionId { get; }

    public WebSocket WebSocket { get; }

    public long? UserId { get; }

    public ConcurrentDictionary<string, string> SubscriptionsById { get; } = new(StringComparer.Ordinal);

    public ConcurrentDictionary<string, string> SubscriptionIdsByDestination { get; } = new(StringComparer.Ordinal);

    public Task SendRawAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (WebSocket.State != WebSocketState.Open)
        {
            return Task.CompletedTask;
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        return WebSocket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    public Task SendHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        return SendRawAsync("\n", cancellationToken);
    }

    public Task SendConnectedAsync(CancellationToken cancellationToken = default)
    {
        var frame = "CONNECTED\nversion:1.2\n\n\0";
        return SendRawAsync(frame, cancellationToken);
    }

    public Task SendErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        var body = message ?? string.Empty;
        var frame = $"ERROR\nmessage:{EscapeHeader(body)}\n\n{body}\0";
        return SendRawAsync(frame, cancellationToken);
    }

    public Task SendMessageAsync(string destination, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return Task.CompletedTask;
        }

        body ??= string.Empty;
        var bytes = Encoding.UTF8.GetBytes(body);
        var frame = $"MESSAGE\ndestination:{EscapeHeader(destination)}\ncontent-type:application/json\ncontent-length:{bytes.Length}\n\n{body}\0";
        return SendRawAsync(frame, cancellationToken);
    }

    private static string EscapeHeader(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace(":", "\\c", StringComparison.Ordinal);
    }
}
