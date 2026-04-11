using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Youlai.Application.Common;
using Youlai.Application.System.Models.User;

namespace Youlai.Application.Common;

/// <summary>
/// SSE 服务实现
/// </summary>
internal sealed class SseService : ISseService, IAsyncDisposable
{
    private readonly ILogger<SseService> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<SseConnection>> _userConnections = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<SseConnection, string> _connectionUser = new();

    public SseService(ILogger<SseService> logger)
    {
        _logger = logger;
    }

    public ChannelReader<SseMessage> CreateConnection(string username, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<SseMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        var connection = new SseConnection
        {
            Username = username,
            Channel = channel,
            ConnectTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        var connections = _userConnections.GetOrAdd(username, _ => new ConcurrentBag<SseConnection>());
        connections.Add(connection);
        _connectionUser[connection] = username;

        _logger.LogInformation("[SSE] User connected: {Username}", username);

        // 连接断开时清理
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 正常断开
            }
            finally
            {
                RemoveConnection(connection);
                _ = BroadcastOnlineCountAsync(default);
            }
        }, CancellationToken.None);

        // 异步广播在线人数
        _ = BroadcastOnlineCountAsync(default);

        return channel.Reader;
    }

    private void RemoveConnection(SseConnection connection)
    {
        if (_connectionUser.TryRemove(connection, out var username))
        {
            if (_userConnections.TryGetValue(username, out var connections))
            {
                connections.TryTake(out _);
                if (connections.IsEmpty)
                {
                    _userConnections.TryRemove(username, out _);
                }
            }
            connection.Channel.Writer.TryComplete();
            _logger.LogInformation("[SSE] User disconnected: {Username}", username);
        }
    }

    public async Task BroadcastDictChangeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dictCode))
        {
            return;
        }

        var payload = new
        {
            dictCode,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        await BroadcastAsync(new SseMessage { EventName = "dict", Data = payload }, cancellationToken);
    }

    public async Task BroadcastOnlineCountAsync(CancellationToken cancellationToken = default)
    {
        var count = GetOnlineUserCount();
        await BroadcastAsync(new SseMessage { EventName = "online-count", Data = count }, cancellationToken);
    }

    public async Task SendToUserAsync(string username, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        if (!_userConnections.TryGetValue(username, out var connections))
        {
            return;
        }

        var message = new SseMessage { EventName = eventName, Data = payload };
        var toRemove = new List<SseConnection>();

        foreach (var connection in connections)
        {
            try
            {
                await connection.Channel.Writer.WriteAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SSE] Failed to send event to user: {Username}", username);
                toRemove.Add(connection);
            }
        }

        foreach (var connection in toRemove)
        {
            RemoveConnection(connection);
        }
    }

    public IReadOnlyList<OnlineUserDto> GetOnlineUsers()
    {
        var result = new List<OnlineUserDto>();
        foreach (var (username, connections) in _userConnections)
        {
            var earliestTime = connections.Min(c => c.ConnectTime);
            result.Add(new OnlineUserDto
            {
                Username = username,
                SessionCount = connections.Count,
                LoginTime = earliestTime,
            });
        }
        return result;
    }

    public int GetOnlineUserCount() => _userConnections.Count;

    private async Task BroadcastAsync(SseMessage message, CancellationToken cancellationToken)
    {
        var toRemove = new List<SseConnection>();

        foreach (var (connection, _) in _connectionUser)
        {
            try
            {
                await connection.Channel.Writer.WriteAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SSE] Failed to broadcast to connection");
                toRemove.Add(connection);
            }
        }

        foreach (var connection in toRemove)
        {
            RemoveConnection(connection);
        }
    }

    private sealed class SseConnection
    {
        public required string Username { get; init; }
        public required Channel<SseMessage> Channel { get; init; }
        public long ConnectTime { get; init; }
    }

    public async ValueTask DisposeAsync()
    {
        var count = _connectionUser.Count;
        if (count == 0) return;

        _logger.LogInformation("[SSE] 应用关闭，主动断开 {Count} 个SSE连接...", count);

        foreach (var connection in _connectionUser.Keys.ToList())
        {
            connection.Channel.Writer.TryComplete();
        }

        _userConnections.Clear();
        _connectionUser.Clear();

        _logger.LogInformation("[SSE] 所有SSE连接已断开");
    }
}
