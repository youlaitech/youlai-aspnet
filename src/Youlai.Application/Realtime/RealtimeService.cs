using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Youlai.Application.System.Models.User;

namespace Youlai.Application.Realtime;

/// <summary>
/// 实时连接会话管理，按用户维护连接并广播主题消息
/// </summary>
internal sealed class RealtimeService : IRealtimeService, IAsyncDisposable
{
    // 单节点连接上限与单用户连接上限，超出后拒绝新连接
    private const int MaxConnections = 5000;
    private const int MaxConnectionsPerUser = 10;

    private readonly ILogger<RealtimeService> _logger;
    private readonly ConcurrentDictionary<string, IRealtimeClient> _clients = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, IRealtimeClient>> _userClients = new();

    /// <summary>节点标识，进程内固定，用于前端区分多节点部署</summary>
    private readonly string _nodeId = Guid.NewGuid().ToString("N");

    /// <summary>在线状态快照广播定时器</summary>
    private readonly Timer _presenceTimer;

    public RealtimeService(ILogger<RealtimeService> logger)
    {
        _logger = logger;

        // 周期广播在线状态快照，前端据此刷新在线人数
        _presenceTimer = new Timer(_ => BroadcastPresence(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// 生成在线状态快照
    /// </summary>
    public RealtimePresenceSnapshot PresenceSnapshot()
    {
        return new RealtimePresenceSnapshot
        {
            OnlineUsers = _userClients.Count,
            OnlineConnections = _clients.Count,
            NodeId = _nodeId,
            ObservedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// 广播在线状态快照
    /// </summary>
    public void BroadcastPresence()
    {
        BroadcastAsync(RealtimeTopics.Presence, RealtimeTopics.EventPresenceSnapshot, PresenceSnapshot());
    }

    /// <summary>
    /// 注册连接，同一客户端实例重连会关闭旧连接
    /// </summary>
    public bool TryRegister(IRealtimeClient client)
    {
        if (_clients.Count >= MaxConnections)
        {
            _logger.LogWarning("[Realtime] 连接数已满，拒绝连接 {ClientId}", client.Id);
            return false;
        }

        var userClients = _userClients.GetOrAdd(client.UserId, _ => new ConcurrentDictionary<string, IRealtimeClient>(StringComparer.Ordinal));

        // 同一客户端实例重连，先关闭该实例的旧连接
        if (!string.IsNullOrEmpty(client.ClientInstanceId))
        {
            var stale = userClients
                .Where(x => !string.Equals(x.Key, client.Id, StringComparison.Ordinal)
                            && string.Equals(x.Value.ClientInstanceId, client.ClientInstanceId, StringComparison.Ordinal))
                .Select(x => x.Value)
                .ToList();
            foreach (var old in stale)
            {
                Unregister(old);
                _ = SafeCloseAsync(old);
            }
        }

        if (userClients.Count >= MaxConnectionsPerUser)
        {
            _logger.LogWarning("[Realtime] 用户 {UserId} 连接数已达上限", client.UserId);
            return false;
        }

        if (!_clients.TryAdd(client.Id, client))
        {
            return false;
        }
        userClients[client.Id] = client;

        _logger.LogInformation("[Realtime] 用户 {UserId} 建立连接 {ClientId}", client.UserId, client.Id);
        return true;
    }

    /// <summary>
    /// 移除连接，用户无连接时清空其记录
    /// </summary>
    public void Unregister(IRealtimeClient client)
    {
        if (!_clients.TryRemove(client.Id, out _))
        {
            return;
        }

        if (_userClients.TryGetValue(client.UserId, out var userClients))
        {
            userClients.TryRemove(client.Id, out _);
            if (userClients.IsEmpty)
            {
                _userClients.TryRemove(client.UserId, out _);
            }
        }

        _logger.LogInformation("[Realtime] 用户 {UserId} 断开连接 {ClientId}", client.UserId, client.Id);
    }

    /// <summary>
    /// 向订阅了该主题的全部连接广播消息
    /// </summary>
    public Task BroadcastAsync(string topic, string type, object? payload, CancellationToken cancellationToken = default)
    {
        var message = RealtimeMessage.Create(type, payload);
        var targets = _clients.Values.Where(c => c.Topics.Contains(topic)).ToList();
        return DispatchAsync(targets, message, cancellationToken);
    }

    /// <summary>
    /// 向指定用户的全部连接推送消息
    /// </summary>
    public Task SendToUserAsync(long userId, string type, object? payload, CancellationToken cancellationToken = default)
    {
        if (!_userClients.TryGetValue(userId, out var userClients))
        {
            return Task.CompletedTask;
        }

        var message = RealtimeMessage.Create(type, payload);
        return DispatchAsync(userClients.Values.ToList(), message, cancellationToken);
    }

    /// <summary>
    /// 获取在线用户列表，登录时间取该用户最早的连接时间
    /// </summary>
    public IReadOnlyList<OnlineUserDto> GetOnlineUsers()
    {
        var result = new List<OnlineUserDto>();
        foreach (var (userId, connections) in _userClients)
        {
            if (connections.IsEmpty)
            {
                continue;
            }
            var earliest = connections.Values.Min(c => c.ConnectedAt);
            result.Add(new OnlineUserDto
            {
                Username = userId.ToString(),
                SessionCount = connections.Count,
                LoginTime = earliest.ToUnixTimeMilliseconds(),
            });
        }
        return result;
    }

    /// <summary>
    /// 在线用户数，按用户去重
    /// </summary>
    public int GetOnlineUserCount() => _userClients.Count;

    /// <summary>
    /// 关闭全部连接
    /// </summary>
    public async Task CloseAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var client in _clients.Values.ToList())
        {
            Unregister(client);
            await SafeCloseAsync(client).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 向连接集合发送消息，发送失败的连接会被移除
    /// </summary>
    private async Task DispatchAsync(IReadOnlyList<IRealtimeClient> targets, RealtimeMessage message, CancellationToken cancellationToken)
    {
        var failed = new List<IRealtimeClient>();

        foreach (var client in targets)
        {
            try
            {
                await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Realtime] 向连接 {ClientId} 发送消息失败", client.Id);
                failed.Add(client);
            }
        }

        foreach (var client in failed)
        {
            Unregister(client);
            await SafeCloseAsync(client).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 应用停机时关闭全部连接
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _presenceTimer.Dispose();
        await CloseAllAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 关闭连接并忽略异常
    /// </summary>
    private async Task SafeCloseAsync(IRealtimeClient client)
    {
        try
        {
            await client.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[Realtime] 关闭连接 {ClientId} 时出错", client.Id);
        }
    }
}
