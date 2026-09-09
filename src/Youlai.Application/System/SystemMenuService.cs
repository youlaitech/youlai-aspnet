using System.Text.Json;
using Youlai.Application.Exceptions;
using Youlai.Application.Models;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.System;
using Youlai.Application.System.Models.Menu;
using Youlai.Domain.Entities;
using Youlai.Domain.Enums;

namespace Youlai.Application.System;

/// <summary>
/// 菜单服务（路由、树形列表、CRUD）
/// </summary>
internal sealed class SystemMenuService : ISystemMenuService
{
    private const string LayoutComponent = "Layout";

    /// <summary>
    /// 菜单下拉选项行
    /// </summary>
    private sealed record MenuOptionRow(long Id, long ParentId, string Name);
    /// <summary>
    /// 菜单列表行
    /// </summary>
    private sealed record MenuRow(long Id, long ParentId, string Name, string Type, string? RouteName, string? RoutePath, string? Component, string? ExternalUrl, int? Sort, int? Visible, string? Icon, string? Redirect, string? Perm);

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<SysMenu> _menuRepo;
    private readonly IRepository<SysRoleMenu> _roleMenuRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermsCacheInvalidator _rolePermsCacheInvalidator;

    public SystemMenuService(
        IReadOnlyDbContext readDb,
        IRepository<SysMenu> menuRepo,
        IRepository<SysRoleMenu> roleMenuRepo,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRolePermsCacheInvalidator rolePermsCacheInvalidator)
    {
        _readDb = readDb;
        _menuRepo = menuRepo;
        _roleMenuRepo = roleMenuRepo;
        _unitOfWork = unitOfWork;
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

        List<SysMenu> menus;

        if (_currentUser.IsRoot)
        {
            menus = await _readDb.ToListAsync(
                _readDb.Query<SysMenu>().Where(m => m.Type != MenuType.Button.GetCode()).OrderBy(m => m.Sort ?? 0),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // EF Core 9+ 将 string[].Contains() 编译为 ReadOnlySpan<string>，MySQL 驱动无法处理 → 必须用 HashSet 兜底
            var roleSet = new HashSet<string>(roleCodes, StringComparer.Ordinal);
            var roleIds = await _readDb.ToListAsync(
                _readDb.Query<SysRole>()
                    .Where(r => r.Status == 1 && !r.IsDeleted && r.Code != null && roleSet.Contains(r.Code))
                    .Select(r => r.Id),
                cancellationToken).ConfigureAwait(false);

            if (roleIds.Count == 0)
            {
                return Array.Empty<RouteVo>();
            }

            var roleIdSet = roleIds.ToHashSet();
            var menuIds = await _readDb.ToListAsync(
                _readDb.Query<SysRoleMenu>().Where(rm => roleIdSet.Contains(rm.RoleId)).Select(rm => rm.MenuId).Distinct(),
                cancellationToken).ConfigureAwait(false);

            var menuIdSet = menuIds.ToHashSet();
            menus = await _readDb.ToListAsync(
                _readDb.Query<SysMenu>().Where(m => m.Type != MenuType.Button.GetCode() && menuIdSet.Contains(m.Id)).OrderBy(m => m.Sort ?? 0),
                cancellationToken).ConfigureAwait(false);
        }

        return BuildRoutes(parentId: 0, menus).ToArray();
    }

    /// <summary>
    /// 取当前用户去重后的角色编码，剔除空值
    /// </summary>
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
    public async Task<IReadOnlyCollection<Option<long>>> ListMenuOptionsAsync(bool onlyParent, CancellationToken cancellationToken = default)
    {
        var q = _readDb.Query<SysMenu>();

        if (onlyParent)
        {
            q = q.Where(m => m.Type != MenuType.Button.GetCode());
        }

        var list = await _readDb.ToListAsync(
            q.OrderBy(m => m.Sort ?? 0).Select(m => new MenuOptionRow(m.Id, m.ParentId, m.Name)),
            cancellationToken).ConfigureAwait(false);

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
        var q = _readDb.Query<SysMenu>();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            q = q.Where(m => m.Name.Contains(keywords));
        }

        var list = await _readDb.ToListAsync(
            q.OrderBy(m => m.Sort ?? 0).Select(m => new MenuRow(
                m.Id, m.ParentId, m.Name, m.Type, m.RouteName, m.RoutePath, m.Component, m.ExternalUrl, m.Sort, m.Visible, m.Icon, m.Redirect, m.Perm)),
            cancellationToken).ConfigureAwait(false);

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
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Id == menuId),
            cancellationToken).ConfigureAwait(false);

        if (entity == null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "菜单不存在");
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
            ExternalUrl = entity.ExternalUrl,
            Params = ParseKeyValueList(entity.Params),
        };
    }

    /// <summary>
    /// 新增或更新菜单
    /// </summary>
    public async Task<bool> SaveMenuAsync(MenuForm formData, CancellationToken cancellationToken = default)
    {
        if (formData.ParentId == formData.Id)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "父级菜单不能是当前菜单");
        }

        var menuType = formData.Type;
        var isExternal = string.Equals(menuType, MenuType.External.GetCode(), StringComparison.Ordinal);
        var isEmbedded = isExternal && string.Equals(formData.Component, "iframe", StringComparison.Ordinal);

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
        else if (isExternal && !isEmbedded)
        {
            component = null;
        }

        var needsRouteName = string.Equals(menuType, MenuType.Menu.GetCode(), StringComparison.Ordinal) || isEmbedded;
        if (needsRouteName)
        {
            if (string.IsNullOrWhiteSpace(routeName))
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "路由名称不能为空");
            }

            var routeNameTrim = routeName.Trim();
            var exists = await _readDb.AnyAsync(
                _readDb.Query<SysMenu>().Where(m => m.RouteName == routeNameTrim && (!formData.Id.HasValue || m.Id != formData.Id.Value)),
                cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "Route name already exists");
            }

            routeName = routeNameTrim;
        }
        else
        {
            routeName = null;
        }

        var treePath = await GenerateMenuTreePathAsync(formData.ParentId, cancellationToken).ConfigureAwait(false);
        var paramsJson = SerializeParams(formData.Params);

        SysMenu entity;
        if (formData.Id.HasValue)
        {
            entity = await _menuRepo.FindByIdAsync(formData.Id.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new BusinessException(ResultCode.InvalidUserInput, "菜单不存在");

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
            entity.ExternalUrl = formData.ExternalUrl;
            entity.Params = paramsJson;
            entity.TreePath = treePath;
        }
        else
        {
            entity = new SysMenu
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
                ExternalUrl = formData.ExternalUrl,
                Params = paramsJson,
                TreePath = treePath,
            };

            _menuRepo.Add(entity);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
    public async Task<bool> RemoveMenuAsync(long menuId, CancellationToken cancellationToken = default)
    {
        // 自身 + 子树（TreePath 包含 ",menuId," / "menuId," / ",menuId"）
        var menuIds = await _readDb.ToListAsync(
            _readDb.Query<SysMenu>().Where(m => m.Id == menuId || (m.TreePath != null && m.TreePath.Contains("," + menuId + ","))).Select(m => m.Id),
            cancellationToken).ConfigureAwait(false);

        if (menuIds.Count == 0)
        {
            return true;
        }

        // 先查关联的角色ID（分步查询避免 EF Core 表达式树编译异常）
        var relatedRoleIds = await _readDb.ToListAsync(
            _readDb.Query<SysRoleMenu>().Where(rm => menuIds.Contains(rm.MenuId)).Select(rm => rm.RoleId).Distinct(),
            cancellationToken).ConfigureAwait(false);

        var roleCodes = await _readDb.ToListAsync(
            _readDb.Query<SysRole>().Where(r => r.Code != null && relatedRoleIds.Contains(r.Id)).Select(r => r.Code!),
            cancellationToken).ConfigureAwait(false);

        var roleMenusToRemove = await _roleMenuRepo.ListAsync(rm => menuIds.Contains(rm.MenuId), cancellationToken).ConfigureAwait(false);
        _roleMenuRepo.RemoveRange(roleMenusToRemove);

        var menusToRemove = await _menuRepo.ListAsync(m => menuIds.Contains(m.Id), cancellationToken).ConfigureAwait(false);
        _menuRepo.RemoveRange(menusToRemove);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (roleCodes.Count > 0)
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(roleCodes, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 修改可见状态
    /// </summary>
    public async Task<bool> UpdateMenuVisibleAsync(long menuId, int visible, CancellationToken cancellationToken = default)
    {
        var entity = await _menuRepo.FindByIdAsync(menuId, cancellationToken).ConfigureAwait(false);

        if (entity == null)
        {
            return false;
        }

        entity.Visible = visible;
        _menuRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 递归构建路由树：按 parentId 匹配父子关系，生成前端 Vue Router 所需的嵌套路由结构
    /// </summary>
    private static List<RouteVo> BuildRoutes(long parentId, List<SysMenu> menus)
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

    /// <summary>
    /// 递归构建菜单下拉选项树，无子节点的项 Children 置空
    /// </summary>
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

    /// <summary>
    /// 递归构建菜单列表树，带 Children 嵌套
    /// </summary>
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
                ExternalUrl = menu.ExternalUrl,
                Children = children.Count > 0 ? children : null,
            });
        }

        return list;
    }

    /// <summary>
    /// 菜单参数 JSON 转键值对列表，解析失败返回 null
    /// </summary>
    private static IReadOnlyCollection<KeyValue>? ParseKeyValueList(JsonElement? element)
    {
        var dict = TryParseParams(element);
        if (dict is null)
        {
            return null;
        }

        return dict.Select(kv => new KeyValue { Key = kv.Key, Value = kv.Value }).ToArray();
    }

    /// <summary>
    /// 键值对列表转 JSON 存入数据库，空列表返回 null
    /// </summary>
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

    /// <summary>
    /// 生成菜单树路径：根节点为 "0"，否则拼成「父路径,父ID」
    /// </summary>
    private async Task<string?> GenerateMenuTreePathAsync(long parentId, CancellationToken cancellationToken)
    {
        if (parentId == 0)
        {
            return "0";
        }

        var parent = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Id == parentId),
            cancellationToken).ConfigureAwait(false);

        return parent == null ? null : string.Concat(parent.TreePath, ",", parent.Id);
    }

    /// <summary>
    /// 父节点路径变化后，递归重写其所有子节点的 TreePath
    /// </summary>
    private async Task UpdateChildrenTreePathAsync(long id, string? treePath, CancellationToken cancellationToken)
    {
        var children = await _menuRepo.ListAsync(m => m.ParentId == id, cancellationToken).ConfigureAwait(false);

        if (children.Count == 0)
        {
            return;
        }

        var childTreePath = string.IsNullOrWhiteSpace(treePath) ? id.ToString() : string.Concat(treePath, ",", id);
        foreach (var child in children)
        {
            child.TreePath = childTreePath;
            _menuRepo.Update(child);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var child in children)
        {
            await UpdateChildrenTreePathAsync(child.Id, childTreePath, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 取关联某菜单的所有角色编码，用于菜单变动后清权限缓存
    /// </summary>
    private async Task<IReadOnlyCollection<string>> GetMenuRelatedRoleCodesAsync(long menuId, CancellationToken cancellationToken)
    {
        var codes = await _readDb.ToListAsync(
            from rm in _readDb.Query<SysRoleMenu>()
            join r in _readDb.Query<SysRole>() on rm.RoleId equals r.Id
            where r.Code != null && rm.MenuId == menuId
            select r.Code!,
            cancellationToken).ConfigureAwait(false);

        return codes;
    }

    /// <summary>
    /// 菜单实体转前端路由 VO：外链/内嵌/目录各有不同的 path、component、name 推导规则
    /// </summary>
    private static RouteVo ToRouteVo(SysMenu menu, IReadOnlyCollection<RouteVo>? children)
    {
        var routePath = menu.RoutePath ?? string.Empty;
        var externalUrl = menu.ExternalUrl;
        var isExternal = string.Equals(menu.Type, MenuType.External.GetCode(), StringComparison.Ordinal);
        var isEmbedded = isExternal && string.Equals(menu.Component, "iframe", StringComparison.Ordinal);

        var path = isExternal && !isEmbedded && !string.IsNullOrWhiteSpace(externalUrl)
            ? externalUrl
            : routePath;

        var routeName = menu.RouteName;
        if (string.IsNullOrWhiteSpace(routeName) && !isExternal)
        {
            if (string.Equals(menu.Type, MenuType.Catalog.GetCode(), StringComparison.Ordinal))
            {
                routeName = path;
            }
            else
            {
                routeName = ToPascalCase(path);
            }
        }

        var meta = new RouteVo.RouteMeta
        {
            Title = menu.Name,
            Icon = string.IsNullOrWhiteSpace(menu.Icon) ? null : menu.Icon,
            Hidden = menu.Visible.HasValue && menu.Visible.Value == 0 ? true : null,
            KeepAlive = (string.Equals(menu.Type, MenuType.Menu.GetCode(), StringComparison.Ordinal) || isEmbedded)
                && menu.KeepAlive == 1 ? true : null,
            AlwaysShow = menu.AlwaysShow == 1 ? true : null,
            Params = TryParseParams(menu.Params),
            ExternalUrl = isEmbedded && !string.IsNullOrWhiteSpace(externalUrl) ? externalUrl : null,
        };

        return new RouteVo
        {
            Name = string.IsNullOrWhiteSpace(routeName) ? null : routeName,
            Path = path,
            Redirect = string.IsNullOrWhiteSpace(menu.Redirect) ? null : menu.Redirect,
            Component = isEmbedded ? "iframe"
                : isExternal ? null
                : (string.IsNullOrWhiteSpace(menu.Component) ? null : menu.Component),
            Meta = meta,
            Children = children,
        };
    }

    /// <summary>
    /// 解析菜单参数 JSON 为字典，空或非法返回 null
    /// </summary>
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

    /// <summary>
    /// 路由路径转大驼峰作为路由名，如 /system/user → SystemUser
    /// </summary>
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
