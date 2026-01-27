using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Youlai.Application.Common.Security;
using Youlai.Infrastructure.Constants;
using Youlai.Infrastructure.Persistence.DbContext;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 角色权限聚合服务（带 Redis 缓存）
/// </summary>
/// <remarks>
/// 聚合角色的权限码列表，并缓存到 Redis，便于鉴权快速判断
/// </remarks>
internal sealed class RolePermissionService : IRolePermissionService
{
    private const string ButtonMenuType = "B";

    private readonly IConnectionMultiplexer _redis;
    private readonly YoulaiDbContext _dbContext;

    public RolePermissionService(IConnectionMultiplexer redis, YoulaiDbContext dbContext)
    {
        _redis = redis;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取角色权限点
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetRolePermsAsync(IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default)
    {
        if (roleCodes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var distinctRoles = roleCodes
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctRoles.Length == 0)
        {
            return Array.Empty<string>();
        }

        // 先从 Redis 批量取缓存，缺失的再回源 DB
        var db = _redis.GetDatabase();
        var roleValues = distinctRoles.Select(r => (RedisValue)r).ToArray();
        var cachedValues = await db.HashGetAsync(RedisKeyConstants.System.RolePerms, roleValues).ConfigureAwait(false);

        var perms = new HashSet<string>(StringComparer.Ordinal);
        var missingRoleCodes = new List<string>();

        for (var i = 0; i < distinctRoles.Length; i++)
        {
            var roleCode = distinctRoles[i];
            var value = cachedValues[i];

            if (value.IsNull)
            {
                missingRoleCodes.Add(roleCode);
                continue;
            }

            var parsed = TryParsePerms(value);
            if (parsed is null || parsed.Count == 0)
            {
                missingRoleCodes.Add(roleCode);
                continue;
            }

            foreach (var p in parsed)
            {
                perms.Add(p);
            }
        }

        if (missingRoleCodes.Count == 0)
        {
            return perms.ToArray();
        }

        // 缓存未命中的角色走数据库聚合
        var dbPerms = await GetRolePermsFromDatabaseAsync(missingRoleCodes, cancellationToken).ConfigureAwait(false);
        foreach (var p in dbPerms.Values.SelectMany(v => v))
        {
            perms.Add(p);
        }

        if (dbPerms.Count > 0)
        {
            // 回写缓存，减少后续查询压力
            var entries = dbPerms
                .Select(kvp => new HashEntry(kvp.Key, JsonSerializer.Serialize(kvp.Value)))
                .ToArray();

            if (entries.Length > 0)
            {
                await db.HashSetAsync(RedisKeyConstants.System.RolePerms, entries).ConfigureAwait(false);
            }
        }

        return perms.ToArray();
    }

    private static IReadOnlyCollection<string>? TryParsePerms(RedisValue value)
    {
        if (value.IsNullOrEmpty)
        {
            return Array.Empty<string>();
        }

        try
        {
            var str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return Array.Empty<string>();
            }

            using var doc = JsonDocument.Parse(str);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return ParseArrayPerms(doc.RootElement);
            }

            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                var s = doc.RootElement.GetString();
                return string.IsNullOrWhiteSpace(s) ? Array.Empty<string>() : new[] { s };
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // 兼容 Spring GenericJackson2JsonRedisSerializer 输出的带类型包装 JSON
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var list = ParseArrayPerms(prop.Value);
                        if (list.Count > 0)
                        {
                            return list;
                        }
                    }
                }
            }

            return Array.Empty<string>();
        }
        catch
        {
            // 不是合法 JSON 时，按单个权限码字符串处理
            var s = value.ToString();
            return string.IsNullOrWhiteSpace(s) ? Array.Empty<string>() : new[] { s };
        }
    }

    private static IReadOnlyCollection<string> ParseArrayPerms(JsonElement arrayElement)
    {
        // 兼容 Spring GenericJackson2JsonRedisSerializer 可能输出的包装数组，例如：
        // ["java.util.HashSet", ["sys:user:list", ...]]
        if (arrayElement.GetArrayLength() == 2
            && arrayElement[0].ValueKind == JsonValueKind.String
            && arrayElement[1].ValueKind == JsonValueKind.Array)
        {
            arrayElement = arrayElement[1];
        }

        var list = new List<string>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s);
                }
            }
        }

        return list;
    }

    private async Task<Dictionary<string, IReadOnlyCollection<string>>> GetRolePermsFromDatabaseAsync(
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var query =
            from rm in _dbContext.SysRoleMenus.AsNoTracking()
            join r in _dbContext.SysRoles.AsNoTracking() on rm.RoleId equals r.Id
            join m in _dbContext.SysMenus.AsNoTracking() on rm.MenuId equals m.Id
            where roleCodes.Contains(r.Code)
                && !r.IsDeleted
                && r.Status == 1
                && m.Type == ButtonMenuType
                && m.Perm != null
            select new { RoleCode = r.Code, Perm = m.Perm };

        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .GroupBy(x => x.RoleCode, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<string>)g
                    .Select(x => x.Perm!)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal
            );
    }
}
