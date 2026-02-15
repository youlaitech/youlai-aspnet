using System.Collections.Concurrent;
using Youlai.Application.Common.Security;

namespace Youlai.Infrastructure.WebSockets;

/// <summary>
/// WebSocket 用户会话注册表
/// 维护WebSocket连接的用户会话信息，支持多设备同时登录。
/// 采用双Dictionary结构实现高效查询。
/// </summary>
public sealed class UserSessionRegistry
{
    /// <summary>
    /// 用户会话映射表
    /// Key: 用户名
    /// Value: 该用户所有WebSocket会话ID集合（支持多设备登录）
    /// </summary>
    private readonly ConcurrentDictionary<string, HashSet<string>> _userSessionsMap = new(StringComparer.Ordinal);

    /// <summary>
    /// 会话详情映射表
    /// Key: WebSocket会话ID
    /// Value: 会话详情（包含用户名、连接时间等）
    /// </summary>
    private readonly ConcurrentDictionary<string, SessionInfo> _sessionDetailsMap = new(StringComparer.Ordinal);

    /// <summary>
    /// 用户上线（建立WebSocket连接）
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="sessionId">WebSocket会话ID</param>
    public void UserConnected(string username, string sessionId)
    {
        var sessions = _userSessionsMap.GetOrAdd(username, _ => new HashSet<string>());
        lock (sessions)
        {
            sessions.Add(sessionId);
        }
        _sessionDetailsMap[sessionId] = new SessionInfo(username, sessionId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// 用户下线（断开所有WebSocket连接）
    /// 移除该用户的所有会话信息
    /// </summary>
    /// <param name="username">用户名</param>
    public void UserDisconnected(string username)
    {
        if (_userSessionsMap.TryRemove(username, out var sessions))
        {
            foreach (var sessionId in sessions)
            {
                _sessionDetailsMap.TryRemove(sessionId, out _);
            }
        }
    }

    /// <summary>
    /// 移除指定会话（单设备下线）
    /// 当用户某一设备断开连接时调用，保留其他设备的会话
    /// </summary>
    /// <param name="sessionId">WebSocket会话ID</param>
    public void RemoveSession(string sessionId)
    {
        if (_sessionDetailsMap.TryRemove(sessionId, out var sessionInfo))
        {
            if (_userSessionsMap.TryGetValue(sessionInfo.Username, out var sessions))
            {
                lock (sessions)
                {
                    sessions.Remove(sessionId);
                    if (sessions.Count == 0)
                    {
                        _userSessionsMap.TryRemove(sessionInfo.Username, out _);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    /// <returns>当前在线用户数（非会话数）</returns>
    public int GetOnlineUserCount() => _userSessionsMap.Count;

    /// <summary>
    /// 获取指定用户的会话数量
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>该用户的WebSocket会话数量（多设备登录时大于1）</returns>
    public int GetUserSessionCount(string username)
    {
        return _userSessionsMap.TryGetValue(username, out var sessions) ? sessions.Count : 0;
    }

    /// <summary>
    /// 获取在线会话总数
    /// </summary>
    /// <returns>所有WebSocket会话的总数（包含多设备）</returns>
    public int GetTotalSessionCount() => _sessionDetailsMap.Count;

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>是否在线（至少有一个活跃会话）</returns>
    public bool IsUserOnline(string username)
    {
        return _userSessionsMap.TryGetValue(username, out var sessions) && sessions.Count > 0;
    }

    /// <summary>
    /// 获取所有在线用户列表
    /// </summary>
    /// <returns>在线用户信息列表</returns>
    public List<OnlineUserDto> GetOnlineUsers()
    {
        var result = new List<OnlineUserDto>();

        foreach (var (username, sessions) in _userSessionsMap)
        {
            long earliestLoginTime = long.MaxValue;
            lock (sessions)
            {
                foreach (var sessionId in sessions)
                {
                    if (_sessionDetailsMap.TryGetValue(sessionId, out var info) && info.ConnectTime < earliestLoginTime)
                    {
                        earliestLoginTime = info.ConnectTime;
                    }
                }
            }

            result.Add(new OnlineUserDto
            {
                Username = username,
                SessionCount = sessions.Count,
                LoginTime = earliestLoginTime == long.MaxValue ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : earliestLoginTime
            });
        }

        return result;
    }
}

/// <summary>
/// WebSocket 会话详情（内部使用）
/// </summary>
internal class SessionInfo
{
    public string Username { get; }
    public string SessionId { get; }
    public long ConnectTime { get; }

    public SessionInfo(string username, string sessionId, long connectTime)
    {
        Username = username;
        SessionId = sessionId;
        ConnectTime = connectTime;
    }
}
