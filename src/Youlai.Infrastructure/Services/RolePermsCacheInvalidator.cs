using StackExchange.Redis;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Constants;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 角色权限缓存失效
/// </summary>
internal sealed class RolePermsCacheInvalidator : IRolePermsCacheInvalidator
{
    private readonly IConnectionMultiplexer _redis;

    public RolePermsCacheInvalidator(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <summary>
    /// 清理指定角色的权限缓存
    /// </summary>
    public async Task InvalidateAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default)
    {
        if (roleCodes.Count == 0)
        {
            return;
        }

        var distinctRoleCodes = roleCodes
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctRoleCodes.Length == 0)
        {
            return;
        }

        var db = _redis.GetDatabase();
        var fields = distinctRoleCodes.Select(r => (RedisValue)r).ToArray();
        await db.HashDeleteAsync(RedisKeyConstants.System.RolePerms, fields).ConfigureAwait(false);
    }
}
