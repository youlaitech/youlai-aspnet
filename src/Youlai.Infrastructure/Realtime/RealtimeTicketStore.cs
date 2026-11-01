using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Youlai.Application.Realtime;

namespace Youlai.Infrastructure.Realtime;

/// <summary>
/// 基于 Redis 的一次性票据存储，取出即删除
/// </summary>
internal sealed class RealtimeTicketStore : IRealtimeTicketStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;
    private readonly ILogger<RealtimeTicketStore> _logger;

    public RealtimeTicketStore(IConnectionMultiplexer connection, ILogger<RealtimeTicketStore> logger)
    {
        _database = connection.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// 保存票据，到期后由 Redis 自动清理
    /// </summary>
    public async Task SaveAsync(string ticket, RealtimeTicketGrant grant, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(grant, JsonOptions);
        await _database.StringSetAsync(Key(ticket), json, ttl).ConfigureAwait(false);
    }

    /// <summary>
    /// 原子取出并删除票据，避免同一票据被并发重复使用
    /// </summary>
    public async Task<RealtimeTicketGrant?> TakeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetDeleteAsync(Key(ticket)).ConfigureAwait(false);
        if (!value.HasValue)
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<RealtimeTicketGrant>(value.ToString(), JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Realtime] 票据内容解析失败，按无效处理");
            return null;
        }
    }

    /// <summary>
    /// Redis 键只保存票据摘要，不暴露可用于握手的票据
    /// </summary>
    private static string Key(string ticket)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
        return "realtime:ticket:" + Convert.ToHexString(digest).ToLowerInvariant();
    }
}
