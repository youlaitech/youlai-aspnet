using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Youlai.Application.Realtime;

namespace Youlai.Api.Realtime;

/// <summary>
/// 处理实时连接握手与消息循环，握手只接受一次性票据，不在地址中传递访问令牌
/// </summary>
public sealed class RealtimeWebSocketMiddleware
{
    private const string WsPath = "/api/v1/realtime/ws";
    private const int MaxMessageBytes = 16_384;

    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan PongTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HeartbeatTick = TimeSpan.FromSeconds(1);
    private const int MaxInboundPerMinute = 60;

    private readonly RequestDelegate _next;
    private readonly ILogger<RealtimeWebSocketMiddleware> _logger;

    public RealtimeWebSocketMiddleware(RequestDelegate next, ILogger<RealtimeWebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 仅接管实时握手路径，其余请求交给后续中间件
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!string.Equals(context.Request.Path, WsPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // 票据服务含作用域依赖，按请求解析，避免中间件被提升为单例
        var sessions = context.RequestServices.GetRequiredService<IRealtimeService>();
        var tickets = context.RequestServices.GetRequiredService<IRealtimeTicketService>();

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected WebSocket request").ConfigureAwait(false);
            return;
        }

        if (!IsOriginAllowed(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var ticket = ReadTicket(context);
        if (ticket is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var grant = await tickets.ConsumeAsync(ticket, context.RequestAborted).ConfigureAwait(false);
        if (grant is null)
        {
            _logger.LogInformation("[Realtime] 握手票据无效或已使用");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var client = new WebSocketRealtimeClient(
            Guid.NewGuid().ToString("N"),
            grant.UserId,
            grant.ClientInstanceId,
            grant.Platform,
            new HashSet<string>(grant.Topics, StringComparer.Ordinal),
            socket);

        if (!sessions.TryRegister(client))
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "connection limit reached", context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            await client.SendAsync(
                RealtimeMessage.Create(RealtimeTopics.EventConnectionReady, new { heartbeatTimeoutMs = RealtimeTopics.HeartbeatTimeoutMs }),
                context.RequestAborted).ConfigureAwait(false);
            await RunAsync(socket, client, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 请求中止，正常结束
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "[Realtime] 连接 {ClientId} 异常断开", client.Id);
        }
        finally
        {
            sessions.Unregister(client);
        }
    }

    /// <summary>
    /// 并发运行接收循环与心跳，任一结束则终止连接
    /// </summary>
    private async Task RunAsync(WebSocket socket, IRealtimeClient client, CancellationToken requestAborted)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);

        var heartbeat = new RealtimeHeartbeat(client, _logger);
        var heartbeatTask = heartbeat.RunAsync(cts.Token);

        try
        {
            await ReceiveLoopAsync(socket, heartbeat, cts).ConfigureAwait(false);
        }
        finally
        {
            cts.Cancel();
            try
            {
                await heartbeatTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 心跳随连接结束
            }
        }
    }

    /// <summary>
    /// 只处理心跳响应，其余消息一律忽略
    /// </summary>
    private async Task ReceiveLoopAsync(WebSocket socket, RealtimeHeartbeat heartbeat, CancellationTokenSource cts)
    {
        var buffer = new byte[4 * 1024];

        while (socket.State == WebSocketState.Open && !cts.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None).ConfigureAwait(false);
                    return;
                }
                message.Write(buffer, 0, result.Count);
                if (message.Length > MaxMessageBytes)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too large", CancellationToken.None)
                        .ConfigureAwait(false);
                    return;
                }
            } while (!result.EndOfMessage);

            if (heartbeat.IsRateExceeded())
            {
                await socket.CloseAsync((WebSocketCloseStatus)4008, "message rate exceeded", CancellationToken.None)
                    .ConfigureAwait(false);
                return;
            }

            var pingId = TryReadPongId(message.ToArray());
            if (pingId is not null)
            {
                heartbeat.ConfirmPong(pingId);
            }
        }
    }

    /// <summary>
    /// 解析客户端心跳响应，非心跳消息返回 null
    /// </summary>
    private static string? TryReadPongId(byte[] payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1)
            {
                return null;
            }
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "heartbeat.pong")
            {
                return null;
            }
            if (!root.TryGetProperty("payload", out var body) || body.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            if (!body.TryGetProperty("pingId", out var pingId) || pingId.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            var value = pingId.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 读取握手票据，地址只接受 ticket 一个查询参数
    /// </summary>
    private static string? ReadTicket(HttpContext context)
    {
        var query = context.Request.Query;
        if (query.Count != 1 || !query.TryGetValue("ticket", out var values) || values.Count != 1)
        {
            return null;
        }
        var ticket = values[0];
        return string.IsNullOrWhiteSpace(ticket) ? null : ticket;
    }

    /// <summary>
    /// 校验来源，浏览器连接必须同源；无来源的非浏览器客户端放行
    /// </summary>
    private static bool IsOriginAllowed(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Origin", out var originValues))
        {
            return true;
        }
        var origin = originValues.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }
        return string.Equals(origin, $"{context.Request.Scheme}://{context.Request.Host}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 单条连接的心跳状态，定时发送 ping 并在超时后关闭连接
    /// </summary>
    private sealed class RealtimeHeartbeat
    {
        private readonly IRealtimeClient _client;
        private readonly ILogger _logger;
        private readonly object _sync = new();
        private readonly Stopwatch _inboundWindow = Stopwatch.StartNew();
        private DateTimeOffset _lastPongAt;
        private DateTimeOffset _nextPingAt;
        private string? _pendingPingId;
        private int _inboundCount;

        public RealtimeHeartbeat(IRealtimeClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
            _lastPongAt = DateTimeOffset.UtcNow;
            _nextPingAt = _lastPongAt.Add(PingInterval);
        }

        /// <summary>
        /// 定时发送心跳并在超时后关闭连接
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(HeartbeatTick, cancellationToken).ConfigureAwait(false);

                    var now = DateTimeOffset.UtcNow;
                    string? pingId = null;
                    var expired = false;

                    lock (_sync)
                    {
                        if (now - _lastPongAt >= PongTimeout)
                        {
                            expired = true;
                        }
                        else if (now >= _nextPingAt && _pendingPingId is null)
                        {
                            pingId = Guid.NewGuid().ToString("N");
                            _pendingPingId = pingId;
                            _nextPingAt = now.Add(PingInterval);
                        }
                    }

                    if (expired)
                    {
                        _logger.LogInformation("[Realtime] 连接 {ClientId} 心跳超时", _client.Id);
                        await _client.CloseAsync().ConfigureAwait(false);
                        return;
                    }

                    if (pingId is null)
                    {
                        continue;
                    }

                    await _client.SendAsync(
                        new RealtimeMessage { Version = 1, Id = pingId, Type = "heartbeat.ping", Payload = new Dictionary<string, object>() },
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 连接结束
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[Realtime] 连接 {ClientId} 心跳终止", _client.Id);
            }
        }

        /// <summary>
        /// 确认心跳响应，仅接受当前待确认的 ping
        /// </summary>
        public void ConfirmPong(string pingId)
        {
            lock (_sync)
            {
                if (!string.Equals(_pendingPingId, pingId, StringComparison.Ordinal))
                {
                    return;
                }
                _pendingPingId = null;
                _lastPongAt = DateTimeOffset.UtcNow;
                _nextPingAt = _lastPongAt.Add(PingInterval);
            }
        }

        /// <summary>
        /// 限制入站消息频率，超出则断开
        /// </summary>
        public bool IsRateExceeded()
        {
            lock (_sync)
            {
                if (_inboundWindow.Elapsed >= TimeSpan.FromMinutes(1))
                {
                    _inboundWindow.Restart();
                    _inboundCount = 0;
                }
                return ++_inboundCount > MaxInboundPerMinute;
            }
        }
    }
}
