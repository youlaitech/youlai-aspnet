using Microsoft.Extensions.Caching.Distributed;
using Youlai.Application.Constants;
using Youlai.Application.Security;

namespace Youlai.Application.Security;

/// <summary>
/// 角色权限缓存失效
/// </summary>
internal sealed class RolePermsCacheInvalidator : IRolePermsCacheInvalidator
{
    private readonly IDistributedCache _cache;

    public RolePermsCacheInvalidator(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 清理指定角色的权限缓存（角色权限按角色编码分键存储）
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

        foreach (var roleCode in distinctRoleCodes)
        {
            await _cache.RemoveAsync(string.Format(RedisKeyConstants.System.RolePerms, roleCode), cancellationToken).ConfigureAwait(false);
        }
    }
}
