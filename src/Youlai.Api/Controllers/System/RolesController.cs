using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.Common.Exceptions;
using Youlai.Application.System.Dtos.Role;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 角色管理接口
/// </summary>
/// <remarks>
/// 提供角色的分页查询、表单、创建、更新、删除及菜单权限分配等能力。
/// </remarks>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
[Tags("03.角色接口")]
public sealed class RolesController : ControllerBase
{
    private readonly ISystemRoleService _roleService;

    public RolesController(ISystemRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// 角色分页
    /// </summary>
    [HttpGet]
    [HasPerm("sys:role:list")]
    public async Task<PageResult<RolePageVo>> GetRolePage([FromQuery] RoleQuery queryParams, CancellationToken cancellationToken)
    {
        return await _roleService.GetRolePageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 角色下拉选项
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetRoleOptions(CancellationToken cancellationToken)
    {
        var list = await _roleService.GetRoleOptionsAsync(cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 新增角色
    /// </summary>
    [HttpPost]
    [HasPerm("sys:role:create")]
    public async Task<Result<object?>> AddRole([FromBody] RoleForm formData, CancellationToken cancellationToken)
    {
        var ok = await _roleService.SaveRoleAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 角色表单
    /// </summary>
    [HttpGet("{roleId:long}/form")]
    [HasPerm("sys:role:update")]
    public async Task<Result<RoleForm>> GetRoleForm([FromRoute] long roleId, CancellationToken cancellationToken)
    {
        var form = await _roleService.GetRoleFormAsync(roleId, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:role:update")]
    public async Task<Result<object?>> UpdateRole([FromRoute] long id, [FromBody] RoleForm formData, CancellationToken cancellationToken)
    {
        if (formData.Id.HasValue && formData.Id.Value != id)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "角色ID不匹配");
        }

        var ok = await _roleService.SaveRoleAsync(new RoleForm
        {
            Id = id,
            Name = formData.Name,
            Code = formData.Code,
            Sort = formData.Sort,
            Status = formData.Status,
            DataScope = formData.DataScope,
            Remark = formData.Remark,
        }, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 批量删除角色
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:role:delete")]
    public async Task<Result<object?>> DeleteRoles([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _roleService.DeleteByIdsAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 修改角色状态
    /// </summary>
    [HttpPut("{roleId:long}/status")]
    [HasPerm("sys:role:update")]
    public async Task<Result<object?>> UpdateRoleStatus([FromRoute] long roleId, [FromQuery] int status, CancellationToken cancellationToken)
    {
        var ok = await _roleService.UpdateRoleStatusAsync(roleId, status, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 角色已分配的菜单ID
    /// </summary>
    [HttpGet("{roleId:long}/menu-ids")]
    public async Task<Result<IReadOnlyCollection<long>>> GetRoleMenuIds([FromRoute] long roleId, CancellationToken cancellationToken)
    {
        var ids = await _roleService.GetRoleMenuIdsAsync(roleId, cancellationToken);
        return Result.Success(ids);
    }

    /// <summary>
    /// 分配菜单
    /// </summary>
    [HttpPut("{roleId:long}/menus")]
    [HasPerm("sys:role:assign")]
    public async Task<Result<object?>> AssignMenusToRole([FromRoute] long roleId, [FromBody] List<long> menuIds, CancellationToken cancellationToken)
    {
        await _roleService.AssignMenusToRoleAsync(roleId, menuIds, cancellationToken);
        return Result.Success();
    }
}
