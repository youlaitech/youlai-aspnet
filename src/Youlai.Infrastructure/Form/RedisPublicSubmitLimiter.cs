using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Youlai.Application.Constants;
using Youlai.Application.Form;

namespace Youlai.Infrastructure.Form;

/// <summary>
/// 公开表单提交限流（Redis 固定窗口计数，Redis 异常时失败放行）
/// </summary>
internal sealed class RedisPublicSubmitLimiter : IPublicSubmitLimiter
{
    /// <summary>单 IP 提交上限（次/分钟）</summary>
    private const int Limit = 10;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPublicSubmitLimiter> _logger;

    public RedisPublicSubmitLimiter(IConnectionMultiplexer redis, ILogger<RedisPublicSubmitLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsAllowedAsync(string formKey, string ip, CancellationToken cancellationToken = default)
    {
        var key = string.Format(RedisKeyConstants.Form.PublicSubmitCount, formKey, ip);
        try
        {
            var database = _redis.GetDatabase();
            var count = await database.StringIncrementAsync(key).ConfigureAwait(false);
            if (count == 1)
            {
                // 首次计数时设置过期，窗口结束后自动清零
                await database.KeyExpireAsync(key, Window).ConfigureAwait(false);
            }

            return count <= Limit;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "公开表单限流 Redis 异常，放行 formKey={FormKey}", formKey);
            return true;
        }
    }
}
