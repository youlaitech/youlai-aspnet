using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Youlai.Application.Auth.Constants;
using Youlai.Application.Common.Services;
using Youlai.Infrastructure.Services;
using Youlai.Infrastructure.WebSockets;

namespace Youlai.Api.WebSockets;

/// <summary>
/// WebSocket 端点（STOMP 协议，路径 /ws）
/// </summary>
/// <remarks>
/// 为前端提供 STOMP over WebSocket 连接能力，支持鉴权、订阅与消息推送
/// </remarks>
internal static class StompWebSocketEndpoint
{
    private const string BearerPrefix = "Bearer ";

    public static async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();

        var broker = context.RequestServices.GetRequiredService<StompBroker>();
        var tokenManager = context.RequestServices.GetRequiredService<JwtTokenManager>();
        var webSocketService = context.RequestServices.GetRequiredService<IWebSocketService>();

        var connectionId = Guid.NewGuid().ToString("N");
        long? userId = null;
        var authenticated = false;

        var connection = new StompConnection(connectionId, socket, userId);
        broker.RegisterConnection(connection);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        var cancellationToken = cts.Token;

        var outgoingHeartbeatMs = 0;
        PeriodicTimer? heartbeatTimer = null;

        try
        {
            var buffer = new byte[16 * 1024];
            var sb = new StringBuilder();

            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                sb.Append(chunk);

                var text = sb.ToString();
                var nullIndex = text.IndexOf('\0');
                if (nullIndex < 0)
                {
                    continue;
                }

                var frames = text.Split('\0');
                sb.Clear();
                if (!text.EndsWith("\0", StringComparison.Ordinal))
                {
                    sb.Append(frames[^1]);
                }

                for (var i = 0; i < frames.Length; i++)
                {
                    var raw = frames[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    if (raw == "\n" || raw == "\r\n")
                    {
                        continue;
                    }

                    var frame = StompFrame.TryParse(raw);
                    if (frame is null)
                    {
                        await connection.SendErrorAsync("Invalid STOMP frame", cancellationToken);
                        continue;
                    }

                    switch (frame.Command)
                    {
                        case "CONNECT":
                        case "STOMP":
                            {
                                var auth = frame.GetHeader("Authorization");
                                var token = ExtractBearerToken(auth);
                                if (string.IsNullOrWhiteSpace(token))
                                {
                                    await connection.SendErrorAsync("Missing Authorization", cancellationToken);
                                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing Authorization", cancellationToken);
                                    return;
                                }

                                if (!tokenManager.TryGetAccessTokenPayload(token, out var payload))
                                {
                                    await connection.SendErrorAsync("Invalid token", cancellationToken);
                                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", cancellationToken);
                                    return;
                                }

                                var uidObj = payload.TryGetValue(JwtClaimConstants.UserId, out var uidRaw) ? uidRaw : null;
                                var uid = TryParseInt64(uidObj);
                                if (uid <= 0)
                                {
                                    await connection.SendErrorAsync("Invalid user", cancellationToken);
                                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid user", cancellationToken);
                                    return;
                                }

                                authenticated = true;
                                userId = uid;

                                broker.UnregisterConnection(connection);
                                connection = new StompConnection(connectionId, socket, userId);
                                broker.RegisterConnection(connection);

                                var heartBeat = frame.GetHeader("heart-beat");
                                if (!string.IsNullOrWhiteSpace(heartBeat))
                                {
                                    var parts = heartBeat.Split(',', StringSplitOptions.TrimEntries);
                                    if (parts.Length == 2 && int.TryParse(parts[1], out var serverOutgoing) && serverOutgoing > 0)
                                    {
                                        outgoingHeartbeatMs = serverOutgoing;
                                    }
                                }

                                await connection.SendConnectedAsync(cancellationToken);

                                await webSocketService.BroadcastOnlineCountAsync(broker.OnlineUserCount, cancellationToken);

                                if (outgoingHeartbeatMs > 0)
                                {
                                    heartbeatTimer?.Dispose();
                                    heartbeatTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(outgoingHeartbeatMs));
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            while (await heartbeatTimer.WaitForNextTickAsync(cancellationToken))
                                            {
                                                await connection.SendHeartbeatAsync(cancellationToken);
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    }, cancellationToken);
                                }

                                break;
                            }
                        case "SUBSCRIBE":
                            {
                                if (!authenticated)
                                {
                                    await connection.SendErrorAsync("Not authenticated", cancellationToken);
                                    continue;
                                }

                                var dest = frame.GetHeader("destination") ?? string.Empty;
                                var id = frame.GetHeader("id") ?? string.Empty;

                                if (string.IsNullOrWhiteSpace(id))
                                {
                                    id = Guid.NewGuid().ToString("N");
                                }

                                broker.Subscribe(connection, id, dest);
                                break;
                            }
                        case "UNSUBSCRIBE":
                            {
                                if (!authenticated)
                                {
                                    await connection.SendErrorAsync("Not authenticated", cancellationToken);
                                    continue;
                                }

                                var id = frame.GetHeader("id") ?? string.Empty;
                                broker.Unsubscribe(connection, id);
                                break;
                            }
                        case "DISCONNECT":
                            {
                                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", cancellationToken);
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            heartbeatTimer?.Dispose();
            broker.UnregisterConnection(connection);

            if (authenticated)
            {
                await webSocketService.BroadcastOnlineCountAsync(broker.OnlineUserCount, CancellationToken.None);
            }

            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                }
            }
            catch
            {
            }
        }
    }

    private static string? ExtractBearerToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var v = value.Trim();
        if (v.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return v[BearerPrefix.Length..].Trim();
        }

        return v;
    }

    private static long TryParseInt64(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is long l)
        {
            return l;
        }

        if (value is int i)
        {
            return i;
        }

        return long.TryParse(value.ToString(), out var parsed) ? parsed : 0;
    }
}

/// <summary>
/// STOMP 帧解析器（/ws 端点内部使用）
/// </summary>
/// <remarks>
/// 用于解析客户端发送的 STOMP 文本帧
/// </remarks>
internal sealed class StompFrame
{
    private StompFrame(string command, Dictionary<string, string> headers, string body)
    {
        Command = command;
        Headers = headers;
        Body = body;
    }

    public string Command { get; }

    public Dictionary<string, string> Headers { get; }

    public string Body { get; }

    public string? GetHeader(string key)
    {
        return Headers.TryGetValue(key, out var v) ? v : null;
    }

    public static StompFrame? TryParse(string raw)
    {
        var text = raw.Replace("\r\n", "\n", StringComparison.Ordinal);
        var parts = text.Split("\n\n", 2, StringSplitOptions.None);
        var headerPart = parts[0];
        var body = parts.Length > 1 ? parts[1] : string.Empty;

        var lines = headerPart.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return null;
        }

        var command = lines[0].Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var idx = line.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }

            var k = line[..idx].Trim();
            var v = line[(idx + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(k))
            {
                headers[k] = v;
            }
        }

        return new StompFrame(command, headers, body);
    }
}
