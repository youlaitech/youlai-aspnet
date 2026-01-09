using Microsoft.EntityFrameworkCore;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Security;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;
using Youlai.Infrastructure.Data;
using Youlai.Domain.Entities;

namespace Youlai.Infrastructure.Services;

/// <summary>
/// 角色服务
/// </summary>
/// <remarks>
/// 提供角色维护、角色下拉选项与角色菜单权限分配能力
/// </remarks>
internal sealed class SystemRoleService : ISystemRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRolePermsCacheInvalidator _rolePermsCacheInvalidator;

    public SystemRoleService(AppDbContext dbContext, ICurrentUser currentUser, IRolePermsCacheInvalidator rolePermsCacheInvalidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _rolePermsCacheInvalidator = rolePermsCacheInvalidator;
    }

    /// <summary>
    /// 角色分页
    /// </summary>
    public async Task<PageResult<RolePageVo>> GetRolePageAsync(RolePageQuery query, CancellationToken cancellationToken = default)
    {
        var pageNum = query.PageNum <= 0 ? 1 : query.PageNum;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var roles = _dbContext.SysRoles
            .AsNoTracking()
            .Where(r => !r.IsDeleted);

        if (!_currentUser.IsRoot)
        {
            roles = roles.Where(r => r.Code != SecurityConstants.RootRoleCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var keywords = query.Keywords.Trim();
            roles = roles.Where(r => (r.Name != null && r.Name.Contains(keywords))
                || (r.Code != null && r.Code.Contains(keywords)));
        }

        roles = roles
            .OrderBy(r => r.Sort ?? 0)
            .ThenByDescending(r => r.CreateTime)
            .ThenByDescending(r => r.UpdateTime);

        var total = await roles.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (total == 0)
        {
            return PageResult<RolePageVo>.Success(Array.Empty<RolePageVo>(), 0, pageNum, pageSize);
        }

        var skip = (pageNum - 1) * pageSize;

        var list = await roles
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new RolePageVo
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Code = r.Code ?? string.Empty,
                Status = r.Status,
                Sort = r.Sort,
                CreateTime = r.CreateTime.HasValue ? r.CreateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                UpdateTime = r.UpdateTime.HasValue ? r.UpdateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PageResult<RolePageVo>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 角色下拉选项
    /// </summary>
    public async Task<IReadOnlyCollection<Option<long>>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        var roles = _dbContext.SysRoles
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == 1);

        if (!_currentUser.IsRoot)
        {
            roles = roles.Where(r => r.Code != SecurityConstants.RootRoleCode);
        }

        var list = await roles
            .OrderBy(r => r.Sort ?? 0)
            .Select(r => new Option<long>(r.Id, r.Name ?? string.Empty))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// 新增或更新角色
    /// </summary>
    public async Task<bool> SaveRoleAsync(RoleForm formData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(formData.Name))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(formData.Code))
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色编码不能为空");
        }

        var name = formData.Name.Trim();
        var code = formData.Code.Trim();

        var roleId = formData.Id;

        SysRole? oldRole = null;
        if (roleId.HasValue)
        {
            oldRole = await _dbContext.SysRoles
                .FirstOrDefaultAsync(r => r.Id == roleId.Value && !r.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (oldRole is null)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "角色不存在");
            }
        }

        var exists = await _dbContext.SysRoles
            .AsNoTracking()
            .AnyAsync(r => !r.IsDeleted
                && (!roleId.HasValue || r.Id != roleId.Value)
                && ((r.Code != null && r.Code == code) || (r.Name != null && r.Name == name)), cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色名称或角色编码已存在，请修改后重试！");
        }

        if (oldRole is null)
        {
            var entity = new SysRole
            {
                Name = name,
                Code = code,
                Sort = formData.Sort,
                Status = formData.Status ?? 1,
                DataScope = formData.DataScope,
                CreateBy = _currentUser.UserId,
                CreateTime = DateTime.Now,
                IsDeleted = false,
            };

            _dbContext.SysRoles.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        var oldCode = oldRole.Code;
        var oldStatus = oldRole.Status;

        oldRole.Name = name;
        oldRole.Code = code;
        oldRole.Sort = formData.Sort;
        oldRole.Status = formData.Status ?? oldRole.Status;
        oldRole.DataScope = formData.DataScope;
        oldRole.UpdateBy = _currentUser.UserId;
        oldRole.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var changedCode = !string.Equals(oldCode, code, StringComparison.Ordinal);
        var changedStatus = oldStatus != oldRole.Status;

        if (changedCode || changedStatus)
        {
            var invalidCodes = new List<string>();
            if (!string.IsNullOrWhiteSpace(oldCode))
            {
                invalidCodes.Add(oldCode);
            }

            invalidCodes.Add(code);
            await _rolePermsCacheInvalidator.InvalidateAsync(invalidCodes, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 角色表单
    /// </summary>
    public async Task<RoleForm> GetRoleFormAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.SysRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色不存在");
        }

        return new RoleForm
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Sort = role.Sort,
            Status = role.Status,
            DataScope = role.DataScope,
            Remark = null,
        };
    }

    /// <summary>
    /// 批量删除角色
    /// </summary>
    public async Task<bool> DeleteByIdsAsync(string ids, CancellationToken cancellationToken = default)
    {
        var roleIds = ParseLongList(ids);
        if (roleIds.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的角色ID不能为空");
        }

        var invalidateCodes = new List<string>();

        foreach (var roleId in roleIds)
        {
            var role = await _dbContext.SysRoles
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken)
                .ConfigureAwait(false);

            if (role is null)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "角色不存在");
            }

            var hasAssignedUsers = await _dbContext.SysUserRoles
                .AsNoTracking()
                .AnyAsync(ur => ur.RoleId == roleId, cancellationToken)
                .ConfigureAwait(false);

            if (hasAssignedUsers)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, $"角色【{role.Name}】已分配用户，请先解除关联后删除");
            }

            role.IsDeleted = true;
            role.UpdateBy = _currentUser.UserId;
            role.UpdateTime = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(role.Code))
            {
                invalidateCodes.Add(role.Code);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (invalidateCodes.Count > 0)
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(invalidateCodes, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 修改角色状态
    /// </summary>
    public async Task<bool> UpdateRoleStatusAsync(long roleId, int status, CancellationToken cancellationToken = default)
    {
        if (status is < 0 or > 1)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "状态值不正确");
        }

        var role = await _dbContext.SysRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色不存在");
        }

        role.Status = status;
        role.UpdateBy = _currentUser.UserId;
        role.UpdateTime = DateTime.Now;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(role.Code))
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(new[] { role.Code }, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// 角色已分配的菜单ID
    /// </summary>
    public async Task<IReadOnlyCollection<long>> GetRoleMenuIdsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.SysRoleMenus
            .AsNoTracking()
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// 分配菜单
    /// </summary>
    public async Task AssignMenusToRoleAsync(long roleId, IReadOnlyCollection<long> menuIds, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.SysRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色不存在");
        }

        var existing = await _dbContext.SysRoleMenus
            .Where(rm => rm.RoleId == roleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            _dbContext.SysRoleMenus.RemoveRange(existing);
        }

        var distinctMenuIds = menuIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        foreach (var menuId in distinctMenuIds)
        {
            _dbContext.SysRoleMenus.Add(new SysRoleMenu
            {
                RoleId = roleId,
                MenuId = menuId,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(role.Code))
        {
            await _rolePermsCacheInvalidator.InvalidateAsync(new[] { role.Code }, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IReadOnlyCollection<long> ParseLongList(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return Array.Empty<long>();
        }

        var list = new List<long>();
        foreach (var s in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (long.TryParse(s, out var id) && id > 0)
            {
                list.Add(id);
            }
        }

        return list;
    }
}
