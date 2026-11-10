using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Youlai.Application.Ai;
using Youlai.Application.Constants;
using Youlai.Application.Exceptions;
using Youlai.Application.Form.Models;
using Youlai.Application.Models;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Application.System;
using Youlai.Application.System.Models.Menu;
using Youlai.Domain.Entities;
using Youlai.Domain.Enums;

namespace Youlai.Application.Form;

/// <summary>
/// 表单定义业务实现（定义 CRUD、发布停用、渲染缓存、访问菜单生成）
/// </summary>
internal sealed class FormDefinitionService : IFormDefinitionService
{
    private const int StatusDraft = 0;
    private const int StatusPublished = 1;
    private const int StatusDisabled = -1;

    /// <summary>默认挂载目录名称：发布向导未显式选目录时自动使用/创建</summary>
    private const string DefaultCatalogName = "表单中心";

    /// <summary>表单管理目录名称（顶级，dynamic-form.sql 预置）</summary>
    private const string FormAdminCatalogName = "动态表单";

    /// <summary>渲染缓存兜底 TTL（分钟）</summary>
    private const int RenderCacheTtlMinutes = 30;

    /// <summary>系统提示词名称，对应 prompts 下的相对路径</summary>
    private const string SystemPromptPath = "form/system.md";

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<FormDefinition> _formRepo;
    private readonly IRepository<FormData> _formDataRepo;
    private readonly IRepository<FormSnapshot> _snapshotRepo;
    private readonly IRepository<SysRoleMenu> _roleMenuRepo;
    private readonly ISystemMenuService _menuService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDistributedCache _cache;
    private readonly IRolePermsCacheInvalidator _rolePermsCacheInvalidator;
    private readonly IAiChatService _aiChatService;
    private readonly IPromptLoader _promptLoader;

    public FormDefinitionService(
        IReadOnlyDbContext readDb,
        IRepository<FormDefinition> formRepo,
        IRepository<FormData> formDataRepo,
        IRepository<FormSnapshot> snapshotRepo,
        IRepository<SysRoleMenu> roleMenuRepo,
        ISystemMenuService menuService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDistributedCache cache,
        IRolePermsCacheInvalidator rolePermsCacheInvalidator,
        IAiChatService aiChatService,
        IPromptLoader promptLoader)
    {
        _readDb = readDb;
        _formRepo = formRepo;
        _formDataRepo = formDataRepo;
        _snapshotRepo = snapshotRepo;
        _roleMenuRepo = roleMenuRepo;
        _menuService = menuService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
        _rolePermsCacheInvalidator = rolePermsCacheInvalidator;
        _aiChatService = aiChatService;
        _promptLoader = promptLoader;
    }

    /// <summary>
    /// 表单定义分页列表
    /// </summary>
    public async Task<PageResult<FormDefinitionPageVo>> GetFormDefinitionPageAsync(FormDefinitionQuery queryParams, CancellationToken cancellationToken = default)
    {
        var (pageNum, pageSize) = queryParams.Normalize();

        var q = _readDb.Query<FormDefinition>();

        if (!string.IsNullOrWhiteSpace(queryParams.Keywords))
        {
            var keywords = queryParams.Keywords.Trim();
            q = q.Where(f => f.FormName.Contains(keywords) || f.FormKey.Contains(keywords));
        }

        if (queryParams.Status.HasValue)
        {
            q = q.Where(f => f.Status == queryParams.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            var category = queryParams.Category.Trim();
            q = q.Where(f => f.Category == category);
        }

        q = q.OrderByDescending(f => f.CreateTime);

        return await _readDb.PageAsync(
            q.Select(f => new FormDefinitionPageVo
            {
                Id = f.Id,
                FormKey = f.FormKey,
                FormName = f.FormName,
                Description = f.Description,
                Status = f.Status,
                Version = f.Version,
                IsPublic = f.IsPublic,
                Category = f.Category,
                CreateTime = f.CreateTime,
            }),
            pageNum, pageSize, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 表单定义表单数据（编辑回显）
    /// </summary>
    public async Task<FormDefinitionForm> GetFormDefinitionFormAsync(long formId, CancellationToken cancellationToken = default)
    {
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");
        }

        return new FormDefinitionForm
        {
            Id = entity.Id,
            FormKey = entity.FormKey,
            FormName = entity.FormName,
            Description = entity.Description,
            FormJson = entity.FormJson,
            OptionsJson = entity.OptionsJson,
            IsPublic = entity.IsPublic,
            Category = entity.Category,
        };
    }

    /// <summary>
    /// 新增表单定义（初始为草稿态）
    /// </summary>
    public async Task<bool> SaveFormDefinitionAsync(FormDefinitionForm formData, CancellationToken cancellationToken = default)
    {
        var formKey = (formData.FormKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(formKey))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单标识不能为空");
        }

        if (string.IsNullOrWhiteSpace(formData.FormName))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单名称不能为空");
        }

        // 校验 formKey 唯一（逻辑删除查询默认过滤，同 key 删除后可重建）
        var exists = await _readDb.AnyAsync(
            _readDb.Query<FormDefinition>().Where(f => f.FormKey == formKey),
            cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单标识已存在");
        }

        var entity = new FormDefinition
        {
            FormKey = formKey,
            FormName = (formData.FormName ?? string.Empty).Trim(),
            Description = formData.Description,
            FormJson = string.IsNullOrWhiteSpace(formData.FormJson) ? "[]" : formData.FormJson!,
            OptionsJson = formData.OptionsJson,
            IsPublic = formData.IsPublic ?? 0,
            Category = string.IsNullOrWhiteSpace(formData.Category) ? "normal" : formData.Category!,
            Status = StatusDraft,
            Version = 1,
        };

        _formRepo.Add(entity);
        return await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 修改表单定义（formKey 创建后不可修改；草稿态可改元数据与规则，非草稿态仅可改规则）
    /// </summary>
    public async Task<bool> UpdateFormDefinitionAsync(long formId, FormDefinitionForm formData, CancellationToken cancellationToken = default)
    {
        var entity = await _formRepo.FindByIdAsync(formId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");
        }

        // formKey 为 null 表示部分更新未携带该字段（如设计器仅保存规则），不视为修改
        if (formData.FormKey is not null && !string.Equals(formData.FormKey, entity.FormKey, StringComparison.Ordinal))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单标识创建后不可修改");
        }

        var wasPublished = entity.Status == StatusPublished;

        // 仅草稿态允许修改元数据
        if (entity.Status == StatusDraft)
        {
            entity.FormName = (formData.FormName ?? entity.FormName).Trim();
            entity.Description = formData.Description;
            entity.Category = string.IsNullOrWhiteSpace(formData.Category) ? entity.Category : formData.Category!;
        }

        entity.FormJson = formData.FormJson ?? entity.FormJson;
        entity.OptionsJson = formData.OptionsJson ?? entity.OptionsJson;
        // 公开开关是访问控制而非身份元数据，任意状态可切换
        entity.IsPublic = formData.IsPublic ?? entity.IsPublic;

        _formRepo.Update(entity);
        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;

        // 已发布表单规则/公开开关变更直接影响渲染端，需清缓存即时生效
        if (ok && wasPublished)
        {
            await EvictRenderCacheAsync(entity.FormKey, cancellationToken).ConfigureAwait(false);
        }

        return ok;
    }

    /// <summary>
    /// 批量删除表单定义（级联清理访问菜单、提交数据与版本快照）
    /// </summary>
    public async Task<bool> RemoveFormDefinitionAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = ParseIds(ids);
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的表单数据为空");
        }

        var forms = await _formRepo.ListAsync(f => idList.Contains(f.Id), cancellationToken).ConfigureAwait(false);
        if (forms.Count == 0)
        {
            return true;
        }

        // 菜单、提交数据、版本快照与表单定义一起删除，避免中途失败留下孤立数据
        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // 级联删除已生成的访问菜单（内含角色授权清理与权限缓存刷新）
        foreach (var form in forms)
        {
            if (form.MenuId.HasValue)
            {
                await _menuService.RemoveMenuAsync(form.MenuId.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        var dataIds = await _readDb.ToListAsync(
            _readDb.Query<FormData>().Where(d => idList.Contains(d.FormId)).Select(d => d.Id),
            cancellationToken).ConfigureAwait(false);
        if (dataIds.Count > 0)
        {
            await _formDataRepo.BatchSoftDeleteAsync(dataIds, _currentUser.UserId, cancellationToken).ConfigureAwait(false);
        }

        var snapshotIds = await _readDb.ToListAsync(
            _readDb.Query<FormSnapshot>().Where(s => idList.Contains(s.FormId)).Select(s => s.Id),
            cancellationToken).ConfigureAwait(false);
        if (snapshotIds.Count > 0)
        {
            await _snapshotRepo.BatchSoftDeleteAsync(snapshotIds, _currentUser.UserId, cancellationToken).ConfigureAwait(false);
        }

        await _formRepo.BatchSoftDeleteAsync(idList, _currentUser.UserId, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        // 已发布表单的渲染缓存一并清除，防止删除后仍可渲染
        foreach (var form in forms.Where(f => f.Status == StatusPublished))
        {
            await EvictRenderCacheAsync(form.FormKey, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 发布表单（版本号+1，固化版本快照，刷新渲染缓存）
    /// </summary>
    public async Task<bool> PublishFormDefinitionAsync(long formId, CancellationToken cancellationToken = default)
    {
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");
        }

        // 版本号与快照必须同时落库，否则会出现已发布但无对应快照
        await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var updated = await _formRepo.BatchUpdateAsync(
            f => f.Id == formId,
            f =>
            {
                f.Status = StatusPublished;
                f.Version += 1;
                f.UpdateBy = _currentUser.UserId;
                f.UpdateTime = DateTime.Now;
            },
            cancellationToken).ConfigureAwait(false);
        if (updated <= 0)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        var published = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);
        if (published is not null)
        {
            // 固化版本快照：数据回显按提交时版本加载，防止规则变更导致历史数据漂移
            _snapshotRepo.Add(new FormSnapshot
            {
                FormId = formId,
                Version = published.Version,
                FormJson = published.FormJson,
                OptionsJson = published.OptionsJson,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (published is not null)
        {
            await EvictRenderCacheAsync(published.FormKey, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 停用表单（清渲染缓存，已发出去的入口立即失效）
    /// </summary>
    public async Task<bool> DisableFormDefinitionAsync(long formId, CancellationToken cancellationToken = default)
    {
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");
        }

        var updated = await _formRepo.BatchUpdateAsync(
            f => f.Id == formId,
            f =>
            {
                f.Status = StatusDisabled;
                f.UpdateBy = _currentUser.UserId;
                f.UpdateTime = DateTime.Now;
            },
            cancellationToken).ConfigureAwait(false);

        if (updated > 0)
        {
            await EvictRenderCacheAsync(entity.FormKey, cancellationToken).ConfigureAwait(false);
        }

        return updated > 0;
    }

    /// <summary>
    /// 按 formKey 获取已发布表单的渲染规则（Redis 缓存）
    /// </summary>
    public async Task<FormRenderVo> GetFormRenderAsync(string formKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RedisKeyConstants.Form.RenderRule, formKey);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedVo = JsonSerializer.Deserialize<FormRenderVo>(cached);
            if (cachedVo is not null)
            {
                return cachedVo;
            }
        }

        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.FormKey == formKey && f.Status == StatusPublished),
            cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在或未发布");
        }

        var renderVo = ToRenderVo(entity);
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(renderVo),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(RenderCacheTtlMinutes) },
            cancellationToken).ConfigureAwait(false);
        return renderVo;
    }

    /// <summary>
    /// 按 formKey 获取公开表单的渲染规则（匿名接口）
    /// </summary>
    public async Task<FormRenderVo> GetPublicFormRenderAsync(string formKey, CancellationToken cancellationToken = default)
    {
        // 直查库不走缓存：缓存 VO 不含 isPublic，命中会绕过公开开关校验
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.FormKey == formKey && f.Status == StatusPublished && f.IsPublic == 1),
            cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在或未开放公开访问");
        }

        return ToRenderVo(entity);
    }

    /// <summary>
    /// 表单已生成的菜单配置（发布向导回显）
    /// </summary>
    public async Task<FormMenuVo?> GetFormMenuAsync(long formId, CancellationToken cancellationToken = default)
    {
        var form = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);
        if (form?.MenuId is null)
        {
            return null;
        }

        var menu = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Id == form.MenuId.Value),
            cancellationToken).ConfigureAwait(false);
        if (menu is null)
        {
            return null;
        }

        var roleIds = await _readDb.ToListAsync(
            _readDb.Query<SysRoleMenu>().Where(rm => rm.MenuId == menu.Id).Select(rm => rm.RoleId).Distinct(),
            cancellationToken).ConfigureAwait(false);

        return new FormMenuVo
        {
            MenuId = menu.Id,
            MenuName = menu.Name,
            ParentId = menu.ParentId,
            RoleIds = roleIds,
        };
    }

    /// <summary>
    /// 生成表单访问菜单并授权可见角色（幂等：已生成时更新原菜单）
    /// </summary>
    public async Task<bool> SaveFormMenuAsync(long formId, FormMenuForm menuForm, CancellationToken cancellationToken = default)
    {
        var form = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == formId),
            cancellationToken).ConfigureAwait(false);
        if (form is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");
        }

        // 上级菜单必须为目录类型；parentId 为空时走默认目录"表单中心"（懒创建）
        SysMenu parentMenu;
        if (menuForm.ParentId is null or 0)
        {
            parentMenu = await EnsureDefaultCatalogAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            parentMenu = await _readDb.FirstOrDefaultAsync(
                _readDb.Query<SysMenu>().Where(m => m.Id == menuForm.ParentId.Value),
                cancellationToken).ConfigureAwait(false) ?? throw new BusinessException(ResultCode.InvalidUserInput, "上级菜单不存在");

            if (!string.Equals(parentMenu.Type, MenuType.Catalog.GetCode(), StringComparison.Ordinal))
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "上级菜单必须为目录类型");
            }
        }

        var routeName = "FormRender" + ToUpperCamel(form.FormKey);
        var menuFormData = new MenuForm
        {
            Id = form.MenuId,
            ParentId = parentMenu.Id,
            Name = menuForm.MenuName,
            Type = MenuType.Menu.GetCode(),
            RouteName = routeName,
            RoutePath = form.FormKey,
            Component = "dynamic-form/render",
            Visible = 1,
            Sort = 1,
            // 多个表单菜单共用 render 组件，不开页面缓存避免切换表单时回显错乱
            KeepAlive = 0,
            Params = new[] { new KeyValue { Key = "formKey", Value = form.FormKey } },
        };

        var ok = await _menuService.SaveMenuAsync(menuFormData, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return false;
        }

        var menu = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.RouteName == routeName),
            cancellationToken).ConfigureAwait(false) ?? throw new BusinessException(ResultCode.SystemError, "菜单生成失败");

        // 回写 menu_id：下次发布走更新语义，防重复生成菜单
        await _formRepo.BatchUpdateAsync(
            f => f.Id == formId,
            f =>
            {
                f.MenuId = menu.Id;
                f.UpdateBy = _currentUser.UserId;
                f.UpdateTime = DateTime.Now;
            },
            cancellationToken).ConfigureAwait(false);

        await AssignMenuToRolesAsync(menu.Id, menuForm.RoleIds, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 审批表单下拉选项（供工作流设计器绑定表单）
    /// </summary>
    public async Task<IReadOnlyCollection<Option<string>>> ListWorkflowFormOptionsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _readDb.ToListAsync(
            _readDb.Query<FormDefinition>()
                .Where(f => f.Category == "workflow" && f.Status == StatusPublished)
                .OrderBy(f => f.CreateTime)
                .Select(f => new { f.FormKey, f.FormName }),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new Option<string>(r.FormKey, r.FormName)).ToList();
    }

    /// <summary>
    /// 按 formKey 查询表单定义（任意状态）
    /// </summary>
    public async Task<FormDefinition?> GetByFormKeyAsync(string formKey, CancellationToken cancellationToken = default)
    {
        return await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.FormKey == formKey),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 用 AI 把需求描述转成 form-create 规则（产出经结构校验后才返回）
    /// </summary>
    public async Task<string> AiGenerateRuleAsync(string description, CancellationToken cancellationToken = default)
    {
        var rules = await _aiChatService.ChatJsonListAsync(
            _promptLoader.Get(SystemPromptPath), "需求描述：\n" + description, cancellationToken).ConfigureAwait(false);

        var json = JsonSerializer.Serialize(rules ?? new List<Dictionary<string, object?>>());
        return FormRuleValidator.ValidateRule(json);
    }

    /// <summary>
    /// 确保默认挂载目录"表单中心"存在，不存在则创建
    /// </summary>
    private async Task<SysMenu> EnsureDefaultCatalogAsync(CancellationToken cancellationToken)
    {
        // 已有"表单中心"目录（任意层级）直接复用：兼容旧环境顶级目录等历史生成位置
        var catalog = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Name == DefaultCatalogName && m.Type == MenuType.Catalog.GetCode()),
            cancellationToken).ConfigureAwait(false);
        if (catalog is not null)
        {
            return catalog;
        }

        var adminCatalog = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>()
                .Where(m => m.ParentId == 0 && m.Name == FormAdminCatalogName && m.Type == MenuType.Catalog.GetCode()),
            cancellationToken).ConfigureAwait(false);
        var underAdminCatalog = adminCatalog is not null;

        // 挂"动态表单"下用相对路由段；退化顶级时用绝对路径，避开 /dynamic-form 前缀
        await _menuService.SaveMenuAsync(new MenuForm
        {
            ParentId = underAdminCatalog ? adminCatalog!.Id : 0,
            Name = DefaultCatalogName,
            Type = MenuType.Catalog.GetCode(),
            RoutePath = underAdminCatalog ? "center" : "/form-center",
            Component = "Layout",
            Visible = 1,
            Sort = 5,
            Icon = "el-icon-EditPen",
        }, cancellationToken).ConfigureAwait(false);

        // 目录类型保存时 routeName 被置空，按名称+类型回查
        var created = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Name == DefaultCatalogName && m.Type == MenuType.Catalog.GetCode()),
            cancellationToken).ConfigureAwait(false);

        return created ?? throw new BusinessException(ResultCode.SystemError, "默认目录创建失败");
    }

    /// <summary>
    /// 追加式角色授权：向角色授予菜单及 tree_path 祖先目录
    /// </summary>
    private async Task AssignMenuToRolesAsync(long menuId, IReadOnlyCollection<long>? roleIds, CancellationToken cancellationToken)
    {
        if (roleIds is null || roleIds.Count == 0)
        {
            return;
        }

        var menu = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<SysMenu>().Where(m => m.Id == menuId),
            cancellationToken).ConfigureAwait(false);
        if (menu is null)
        {
            return;
        }

        // 授权集合 = 菜单本身 + tree_path 祖先目录（根节点 0 排除）
        var menuIdsToGrant = new HashSet<long> { menuId };
        if (!string.IsNullOrWhiteSpace(menu.TreePath))
        {
            foreach (var part in menu.TreePath.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part != "0" && long.TryParse(part, out var ancestorId))
                {
                    menuIdsToGrant.Add(ancestorId);
                }
            }
        }

        var existing = await _readDb.ToListAsync(
            _readDb.Query<SysRoleMenu>().Where(rm => roleIds.Contains(rm.RoleId) && menuIdsToGrant.Contains(rm.MenuId)),
            cancellationToken).ConfigureAwait(false);
        var existingKeys = existing.Select(rm => (rm.RoleId, rm.MenuId)).ToHashSet();

        foreach (var roleId in roleIds)
        {
            foreach (var grantMenuId in menuIdsToGrant)
            {
                if (!existingKeys.Contains((roleId, grantMenuId)))
                {
                    _roleMenuRepo.Add(new SysRoleMenu { RoleId = roleId, MenuId = grantMenuId });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // 授权变化后刷新角色权限缓存
        var roleCodes = await _readDb.ToListAsync(
            _readDb.Query<SysRole>().Where(r => r.Code != null && roleIds.Contains(r.Id)).Select(r => r.Code!),
            cancellationToken).ConfigureAwait(false);
        if (roleCodes.Count > 0)
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(roleCodes, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 组装表单渲染对象
    /// </summary>
    private static FormRenderVo ToRenderVo(FormDefinition entity)
    {
        return new FormRenderVo
        {
            FormKey = entity.FormKey,
            FormName = entity.FormName,
            Version = entity.Version,
            FormJson = entity.FormJson,
            OptionsJson = entity.OptionsJson,
        };
    }

    /// <summary>
    /// 清除表单渲染缓存
    /// </summary>
    private async Task EvictRenderCacheAsync(string formKey, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(string.Format(RedisKeyConstants.Form.RenderRule, formKey), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 表单标识转大驼峰，用于生成全局唯一的路由名称
    /// </summary>
    private static string ToUpperCamel(string value)
    {
        var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>
    /// 解析逗号拼接的 ID 串
    /// </summary>
    private static List<long> ParseIds(string ids)
    {
        return (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.TryParse(s, out var value) ? value : 0)
            .Where(value => value > 0)
            .Distinct()
            .ToList();
    }
}
