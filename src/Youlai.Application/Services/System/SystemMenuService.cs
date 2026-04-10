using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Youlai.Domain.Enums;
using Youlai.Application.Exceptions;
using Youlai.Application.Models;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.System.Dtos.Menu;
using Youlai.Application.System.Interfaces;
using Youlai.Application.Persistence;

namespace Youlai.Application.Services.System;

/// <summary>
/// 菜单服务
/// </summary>
/// <remarks>
/// 提供菜单维护、菜单树查询以及当前用户路由构建能力
/// </remarks>
internal sealed class SystemMenuService : ISystemMenuService
{
    private const string LayoutComponent = "Layout";

    private sealed record MenuOptionRow(long Id, long ParentId, string Name);
    private sealed record MenuRow(long Id, long ParentId, string Name, string Type, string? RouteName, string? RoutePath, string? Component, int? Sort, int? Visible, string? Icon, string? Redirect, string? Perm);

    private readonly IYoulaiDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermsCacheInvalidator _rolePermsCacheInvalidator;

    public SystemMenuService(IYoulaiDbContext dbContext, ICurrentUser currentUser, IRolePermsCacheInvalidator rolePermsCacheInvalidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _rolePermsCacheInvalidator = rolePermsCacheInvalidator;
    }

    /// <summary>
    /// 当前用户路由
    /// </summary>
    public async Task<IReadOnlyCollection<RouteVo>> GetCurrentUserRoutesAsync(CancellationToken cancellationToken = default)
    {
        var roleCodes = GetRoleCodes();
        if (roleCodes.Count == 0)
        {
            return Array.Empty<RouteVo>();
        }

        List<Domain.Entities.SysMenu> menus;

        if (_currentUser.IsRoot)
        {
            menus = await _dbContext.SysMenus
                .AsNoTracking()
                .Where(m => m.Type != MenuType.Button.GetCode())
                .OrderBy(m => m.Sort ?? 0)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // Use parameterized LINQ query instead of raw SQL to prevent SQL injection
            var roleCodesSet = roleCodes.ToHashSet(StringComparer.Ordinal);
            menus = await (
                from m in _dbContext.SysMenus.AsNoTracking()
                join rm in _dbContext.SysRoleMenus on m.Id equals rm.MenuId
                join r in _dbContext.SysRoles on rm.RoleId equals r.Id
                where m.Type != MenuType.Button.GetCode()
                    && r.Status == 1 && !r.IsDeleted
                    && roleCodesSet.Contains(r.Code)
                select m
            ).Distinct()
             .OrderBy(m => m.Sort ?? 0)
             .ToListAsync(cancellationToken)
             .ConfigureAwait(false);
        }

        return BuildRoutes(parentId: 0, menus).ToArray();
    }

    private List<string> GetRoleCodes()
    {
        var roles = _currentUser.Roles;
        if (roles == null || roles.Count == 0)
        {
            return new List<string>();
        }

        return roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 菜单下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<long>>> GetMenuOptionsAsync(bool onlyParent, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.SysMenus
            .AsNoTracking();

        if (onlyParent)
        {
            q = q.Where(m => m.Type != MenuType.Button.GetCode());
        }

        var list = await q
            .OrderBy(m => m.Sort ?? 0)
            .Select(m => new MenuOptionRow(m.Id, m.ParentId, m.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (list.Count == 0)
        {
            return Array.Empty<Option<long>>();
        }

        var menuIds = list.Select(m => m.Id).ToHashSet();
        var parentIds = list.Select(m => m.ParentId).ToHashSet();
        var rootIds = parentIds.Where(pid => !menuIds.Contains(pid)).ToArray();

        var result = new List<Option<long>>();
        foreach (var rootId in rootIds)
        {
            result.AddRange(RecurMenuOptions(rootId, list));
        }

        return result;
    }

    /// <summary>
    /// 菜单列表
    /// </summary>
    public async Task<IReadOnlyCollection<MenuVo>> GetMenuListAsync(MenuQuery query, CancellationToken cancellationToken = default)
    {
        var q = _dbContext.SysMenus
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            q = q.Where(m => m.Name.Contains(keywords));
        }

        var list = await q
            .OrderBy(m => m.Sort ?? 0)
            .Select(m => new MenuRow(
                m.Id,
                m.ParentId,
                m.Name,
                m.Type,
                m.RouteName,
                m.RoutePath,
                m.Component,
                m.Sort,
                m.Visible,
                m.Icon,
                m.Redirect,
                m.Perm
            ))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (list.Count == 0)
        {
            return Array.Empty<MenuVo>();
        }

        var menuIds = list.Select(m => m.Id).ToHashSet();
        var parentIds = list.Select(m => m.ParentId).ToHashSet();
        var rootIds = parentIds.Where(pid => !menuIds.Contains(pid)).ToArray();

        var result = new List<MenuVo>();
        foreach (var rootId in rootIds)
        {
            result.AddRange(RecurMenuList(rootId, list));
        }

        return result;
    }

    /// <summary>
    /// 菜单表单
    /// </summary>
    public async Task<MenuForm> GetMenuFormAsync(long menuId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysMenus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menuId, cancellationToken)
            .ConfigureAwait(false);

        if (entity == null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "菜单不存�?);
        }

        return new MenuForm
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Name = entity.Name,
            Type = entity.Type,
            RouteName = entity.RouteName,
            RoutePath = entity.RoutePath,
            Component = entity.Component,
            Perm = entity.Perm,
            Visible = entity.Visible,
            Sort = entity.Sort,
            Icon = entity.Icon,
            Redirect = entity.Redirect,
            KeepAlive = entity.KeepAlive,
            AlwaysShow = entity.AlwaysShow,
            Params = ParseKeyValueList(entity.Params),
        };
    }

    /// <summary>
    /// 新增或更新菜�?    /// </summary>
    public async Task<bool> SaveMenuAsync(MenuForm formData, CancellationToken cancellationToken = default)
    {
        if (formData.ParentId == formData.Id)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "父级菜单不能为当前菜�?);
        }

        var menuType = formData.Type;
        // 外链菜单不挂载组件，仅保留跳转地址
        var isExternalLink = string.Equals(menuType, MenuType.Menu.GetCode(), StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(formData.RoutePath)
            && (formData.RoutePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || formData.RoutePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        var routePath = formData.RoutePath;
        var component = formData.Component;
        var routeName = formData.RouteName;

        if (string.Equals(menuType, MenuType.Catalog.GetCode(), StringComparison.Ordinal))
        {
            if (formData.ParentId == 0 && !string.IsNullOrWhiteSpace(routePath) && !routePath.StartsWith("/", StringComparison.Ordinal))
            {
                routePath = "/" + routePath;
            }

            component = LayoutComponent;
        }
        else if (isExternalLink)
        {
            component = null;
        }

        if (string.Equals(menuType, MenuType.Menu.GetCode(), StringComparison.Ordinal) && !isExternalLink)
        {
            // 仅菜单类型且非外链时校验路由名唯一
            if (string.IsNullOrWhiteSpace(routeName))
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "路由名称不能为空");
            }

            var routeNameTrim = routeName.Trim();
            var exists = await _dbContext.SysMenus
                .AsNoTracking()
                .AnyAsync(m => m.RouteName == routeNameTrim && (!formData.Id.HasValue || m.Id != formData.Id.Value), cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "路由名称已存�?);
            }

            routeName = routeNameTrim;
        }
        else
        {
            routeName = null;
        }

        // 生成树路径，后续用于更新子节�?        var treePath = await GenerateMenuTreePathAsync(formData.ParentId, cancellationToken).ConfigureAwait(false);
        var paramsJson = SerializeParams(formData.Params);

        Domain.Entities.SysMenu entity;
        if (formData.Id.HasValue)
        {
            entity = await _dbContext.SysMenus
                .FirstOrDefaultAsync(m => m.Id == formData.Id.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new BusinessException(ResultCode.InvalidUserInput, "菜单不存�?);

            entity.ParentId = formData.ParentId;
            entity.Name = formData.Name.Trim();
            entity.Type = menuType;
            entity.RouteName = routeName;
            entity.RoutePath = routePath;
            entity.Component = component;
            entity.Perm = formData.Perm;
            entity.Visible = formData.Visible;
            entity.Sort = formData.Sort;
            entity.Icon = formData.Icon;
            entity.Redirect = formData.Redirect;
            entity.KeepAlive = formData.KeepAlive;
            entity.AlwaysShow = formData.AlwaysShow;
            entity.Params = paramsJson;
            entity.TreePath = treePath;
        }
        else
        {
            entity = new Domain.Entities.SysMenu
            {
                ParentId = formData.ParentId,
                Name = formData.Name.Trim(),
                Type = menuType,
                RouteName = routeName,
                RoutePath = routePath,
                Component = component,
                Perm = formData.Perm,
                Visible = formData.Visible,
                Sort = formData.Sort,
                Icon = formData.Icon,
                Redirect = formData.Redirect,
                KeepAlive = formData.KeepAlive,
                AlwaysShow = formData.AlwaysShow,
                Params = paramsJson,
                TreePath = treePath,
            };

            await _dbContext.SysMenus.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await UpdateChildrenTreePathAsync(entity.Id, treePath, cancellationToken).ConfigureAwait(false);

        if (formData.Id.HasValue)
        {
            var roleCodes = await GetMenuRelatedRoleCodesAsync(entity.Id, cancellationToken).ConfigureAwait(false);
            if (roleCodes.Count > 0)
            {
                await _rolePermsCacheInvalidator.InvalidateAsync(roleCodes, cancellationToken).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    public async Task<bool> DeleteMenuAsync(long menuId, CancellationToken cancellationToken = default)
    {
        // 一次性拿到自身及所有子级菜�?        var menuIds = await _dbContext.SysMenus
            .AsNoTracking()
            .Where(m => m.Id == menuId
                || (m.TreePath != null
                    && EF.Functions.Like(
                        string.Concat(",", m.TreePath, ","),
                        string.Concat("%,", menuId, ",%")
                    )))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (menuIds.Count == 0)
        {
            return true;
        }

        // 先收集受影响的角色编码，便于刷新权限缓存
        var roleCodes = await (
                from rm in _dbContext.SysRoleMenus.AsNoTracking()
                join r in _dbContext.SysRoles.AsNoTracking() on rm.RoleId equals r.Id
                where r.Code != null && menuIds.Contains(rm.MenuId)
                select r.Code
            )
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // 先删关联表，再删菜单本体
        _dbContext.SysRoleMenus.RemoveRange(_dbContext.SysRoleMenus.Where(x => menuIds.Contains(x.MenuId)));
        _dbContext.SysMenus.RemoveRange(_dbContext.SysMenus.Where(x => menuIds.Contains(x.Id)));

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (roleCodes.Count > 0)
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(roleCodes, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 修改可见状�?    /// </summary>
    public async Task<bool> UpdateMenuVisibleAsync(long menuId, int visible, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SysMenus
            .FirstOrDefaultAsync(m => m.Id == menuId, cancellationToken)
            .ConfigureAwait(false);

        if (entity == null)
        {
            return false;
        }

        entity.Visible = visible;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static List<RouteVo> BuildRoutes(long parentId, List<Domain.Entities.SysMenu> menus)
    {
        var routes = new List<RouteVo>();

        foreach (var menu in menus)
        {
            if (menu.ParentId != parentId)
            {
                continue;
            }

            var children = BuildRoutes(menu.Id, menus);
            routes.Add(ToRouteVo(menu, children.Count > 0 ? children : null));
        }

        return routes;
    }

    private static IReadOnlyCollection<Option<long>> RecurMenuOptions(long parentId, IReadOnlyCollection<MenuOptionRow> menus)
    {
        var list = new List<Option<long>>();

        foreach (var menu in menus)
        {
            if (menu.ParentId != parentId)
            {
                continue;
            }

            var children = RecurMenuOptions(menu.Id, menus);
            list.Add(new Option<long>
            {
                Value = menu.Id,
                Label = menu.Name,
                Children = children.Count > 0 ? children : null,
            });
        }

        return list;
    }

    private static IReadOnlyCollection<MenuVo> RecurMenuList(long parentId, IReadOnlyCollection<MenuRow> menus)
    {
        var list = new List<MenuVo>();

        foreach (var menu in menus)
        {
            if (menu.ParentId != parentId)
            {
                continue;
            }

            var children = RecurMenuList(menu.Id, menus);
            list.Add(new MenuVo
            {
                Id = menu.Id,
                ParentId = menu.ParentId,
                Name = menu.Name,
                Type = menu.Type,
                RouteName = menu.RouteName,
                RoutePath = menu.RoutePath,
                Component = menu.Component,
                Sort = menu.Sort,
                Visible = menu.Visible,
                Icon = menu.Icon,
                Redirect = menu.Redirect,
                Perm = menu.Perm,
                Children = children.Count > 0 ? children : null,
            });
        }

        return list;
    }

    private static IReadOnlyCollection<KeyValue>? ParseKeyValueList(JsonElement? element)
    {
        var dict = TryParseParams(element);
        if (dict is null)
        {
            return null;
        }

        return dict.Select(kv => new KeyValue { Key = kv.Key, Value = kv.Value }).ToArray();
    }

    private static JsonElement? SerializeParams(IReadOnlyCollection<KeyValue>? list)
    {
        if (list == null || list.Count == 0)
        {
            return null;
        }

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in list)
        {
            if (string.IsNullOrWhiteSpace(kv.Key))
            {
                continue;
            }

            dict[kv.Key.Trim()] = kv.Value?.Trim() ?? string.Empty;
        }

        if (dict.Count == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(dict);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private async Task<string?> GenerateMenuTreePathAsync(long parentId, CancellationToken cancellationToken)
    {
        if (parentId == 0)
        {
            return "0";
        }

        var parent = await _dbContext.SysMenus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == parentId, cancellationToken)
            .ConfigureAwait(false);

        return parent == null ? null : string.Concat(parent.TreePath, ",", parent.Id);
    }

    private async Task UpdateChildrenTreePathAsync(long id, string? treePath, CancellationToken cancellationToken)
    {
        var children = await _dbContext.SysMenus
            .Where(m => m.ParentId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (children.Count == 0)
        {
            return;
        }

        var childTreePath = string.IsNullOrWhiteSpace(treePath) ? id.ToString() : string.Concat(treePath, ",", id);
        foreach (var child in children)
        {
            child.TreePath = childTreePath;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var child in children)
        {
            await UpdateChildrenTreePathAsync(child.Id, childTreePath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyCollection<string>> GetMenuRelatedRoleCodesAsync(long menuId, CancellationToken cancellationToken)
    {
        var codes = await (
                from rm in _dbContext.SysRoleMenus.AsNoTracking()
                join r in _dbContext.SysRoles.AsNoTracking() on rm.RoleId equals r.Id
                where r.Code != null && rm.MenuId == menuId
                select r.Code
            )
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return codes;
    }

    private static RouteVo ToRouteVo(Domain.Entities.SysMenu menu, IReadOnlyCollection<RouteVo>? children)
    {
        var routePath = menu.RoutePath ?? string.Empty;
        var externalLink = routePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || routePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        var routeName = menu.RouteName;
        if (string.IsNullOrWhiteSpace(routeName))
        {
            if (externalLink)
            {
                routeName = $"ext-{menu.Id}";
            }
            else if (string.Equals(menu.Type, MenuType.Catalog.GetCode(), StringComparison.Ordinal))
            {
                // 目录使用路由路径作为名称，避免与子菜单路由名称冲�?                routeName = routePath;
            }
            else
            {
                routeName = ToPascalCase(routePath);
            }
        }

        var meta = new RouteVo.RouteMeta
        {
            Title = menu.Name,
            Icon = string.IsNullOrWhiteSpace(menu.Icon) ? null : menu.Icon,
            Hidden = menu.Visible.HasValue && menu.Visible.Value == 0 ? true : null,
            KeepAlive = string.Equals(menu.Type, MenuType.Menu.GetCode(), StringComparison.Ordinal) && menu.KeepAlive == 1 ? true : null,
            AlwaysShow = menu.AlwaysShow == 1 ? true : null,
            Params = TryParseParams(menu.Params),
        };

        return new RouteVo
        {
            Name = string.IsNullOrWhiteSpace(routeName) ? null : routeName,
            Path = routePath,
            Redirect = string.IsNullOrWhiteSpace(menu.Redirect) ? null : menu.Redirect,
            Component = externalLink ? null : (string.IsNullOrWhiteSpace(menu.Component) ? null : menu.Component),
            Meta = meta,
            Children = children,
        };
    }

    private static IReadOnlyDictionary<string, string>? TryParseParams(JsonElement? jsonElement)
    {
        if (jsonElement is not { } element)
        {
            return null;
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(element.GetRawText());
            return dict is { Count: > 0 } ? dict : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ToPascalCase(string routePath)
    {
        var s = routePath.Trim();
        if (s.StartsWith("/", StringComparison.Ordinal))
        {
            s = s[1..];
        }

        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        var parts = s
            .Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
