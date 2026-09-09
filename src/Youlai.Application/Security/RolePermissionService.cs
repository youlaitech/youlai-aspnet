using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Youlai.Application.Constants;
using Youlai.Application.Persistence;
using Youlai.Application.Security;
using Youlai.Domain.Entities;

namespace Youlai.Application.Security;

/// <summary>
/// 角色权限服务（聚合权限码 + 分布式缓存）
/// </summary>
internal sealed class RolePermissionService : IRolePermissionService
{
    private const string ButtonMenuType = "B";

    private readonly IDistributedCache _cache;
    private readonly IReadOnlyDbContext _readDb;

    public RolePermissionService(IDistributedCache cache, IReadOnlyDbContext readDb)
    {
        _cache = cache;
        _readDb = readDb;
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

        // 逐角色从缓存取权限，未命中的再回源 DB
        var perms = new HashSet<string>(StringComparer.Ordinal);
        var missingRoleCodes = new List<string>();

        foreach (var roleCode in distinctRoles)
        {
            var cached = await _cache.GetStringAsync(string.Format(RedisKeyConstants.System.RolePerms, roleCode), cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(cached))
            {
                missingRoleCodes.Add(roleCode);
                continue;
            }

            var parsed = TryParsePerms(cached);
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
            // 回写缓存，减少后续查询压力（按角色编码分键）
            foreach (var kvp in dbPerms)
            {
                await _cache.SetStringAsync(string.Format(RedisKeyConstants.System.RolePerms, kvp.Key), JsonSerializer.Serialize(kvp.Value), cancellationToken).ConfigureAwait(false);
            }
        }

        return perms.ToArray();
    }

    /// <summary>
    /// 解析缓存中的权限 JSON 数组，解析失败返回空集合
    /// </summary>
    private static IReadOnlyCollection<string>? TryParsePerms(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<string>();
        }

        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            using var doc = JsonDocument.Parse(value);

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

    /// <summary>
    /// 取出 JSON 数组中的非空字符串项
    /// </summary>
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

    /// <summary>
    /// 按角色编码聚合按钮类菜单的权限码，返回「角色编码 → 权限码集合」
    /// </summary>
    private async Task<Dictionary<string, IReadOnlyCollection<string>>> GetRolePermsFromDatabaseAsync(
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var roleSet = roleCodes.ToHashSet(StringComparer.Ordinal);

        var roles = await _readDb.ToListAsync(
            _readDb.Query<SysRole>()
                .Where(r => r.Code != null && !r.IsDeleted && r.Status == 1 && roleSet.Contains(r.Code))
                .Select(r => new { r.Id, Code = r.Code! }),
            cancellationToken).ConfigureAwait(false);

        if (roles.Count == 0)
        {
            return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        }

        var roleIdToCode = roles.ToDictionary(r => r.Id, r => r.Code);
        var roleIds = roles.Select(r => r.Id).ToList();

        // 第二步：查询角色菜单关联和权限
        var rows = await _readDb.ToListAsync(
            (from rm in _readDb.Query<SysRoleMenu>()
             join m in _readDb.Query<SysMenu>() on rm.MenuId equals m.Id
             where roleIds.Contains(rm.RoleId)
                 && m.Type == ButtonMenuType
                 && m.Perm != null
             select new { rm.RoleId, Perm = m.Perm! }),
            cancellationToken).ConfigureAwait(false);

        return rows
            .Where(x => roleIdToCode.ContainsKey(x.RoleId))
            .GroupBy(x => roleIdToCode[x.RoleId], StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<string>)g
                    .Select(x => x.Perm)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal
            );
    }
}
