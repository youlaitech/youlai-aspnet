using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Youlai.Application.Security;
using Youlai.Application.Constants;
using Youlai.Application.Persistence;

namespace Youlai.Application.Security;

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
    private readonly IDbContext _dbContext;

    public RolePermissionService(IConnectionMultiplexer redis, IDbContext dbContext)
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

            return Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyCollection<string> ParseArrayPerms(JsonElement arrayElement)
    {
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
        var roleCodesArray = roleCodes.ToArray();

        // 第一步：查询符合条件的角色（单表 Contains，避免 EF Core 表达式树编译异常）
        var roles = await _dbContext.SysRoles
            .AsNoTracking()
            .Where(r => roleCodesArray.Contains(r.Code) && !r.IsDeleted && r.Status == 1)
            .Select(r => new { r.Id, r.Code })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (roles.Count == 0)
        {
            return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        }

        var roleIdToCode = roles.ToDictionary(r => r.Id, r => r.Code);
        var roleIds = roles.Select(r => r.Id).ToList();

        // 第二步：查询角色菜单关联和权限
        var rows = await (
            from rm in _dbContext.SysRoleMenus.AsNoTracking()
            join m in _dbContext.SysMenus.AsNoTracking() on rm.MenuId equals m.Id
            where roleIds.Contains(rm.RoleId)
                && m.Type == ButtonMenuType
                && m.Perm != null
            select new { rm.RoleId, Perm = m.Perm }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .Where(x => roleIdToCode.ContainsKey(x.RoleId))
            .Select(x => new { RoleCode = roleIdToCode[x.RoleId], x.Perm })
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
