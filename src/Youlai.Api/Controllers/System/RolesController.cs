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
/// 瑙掕壊绠＄悊鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵瑙掕壊鐨勫垎椤垫煡璇€佽鎯呫€佸垱寤恒€佷慨鏀广€佸垹闄や互鍙婅彍鍗曟潈闄愬垎閰嶇瓑鑳藉姏
/// </remarks>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly ISystemRoleService _roleService;

    public RolesController(ISystemRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// 瑙掕壊鍒嗛〉
    /// </summary>
    [HttpGet]
    [HasPerm("sys:role:list")]
    public async Task<PageResult<RolePageVo>> GetRolePage([FromQuery] RoleQuery queryParams, CancellationToken cancellationToken)
    {
        return await _roleService.GetRolePageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 瑙掕壊涓嬫媺閫夐」
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetRoleOptions(CancellationToken cancellationToken)
    {
        var list = await _roleService.GetRoleOptionsAsync(cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 鏂板瑙掕壊
    /// </summary>
    [HttpPost]
    [HasPerm("sys:role:create")]
    public async Task<Result<object?>> AddRole([FromBody] RoleForm formData, CancellationToken cancellationToken)
    {
        var ok = await _roleService.SaveRoleAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 瑙掕壊琛ㄥ崟
    /// </summary>
    [HttpGet("{roleId:long}/form")]
    [HasPerm("sys:role:update")]
    public async Task<Result<RoleForm>> GetRoleForm([FromRoute] long roleId, CancellationToken cancellationToken)
    {
        var form = await _roleService.GetRoleFormAsync(roleId, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 鏇存柊瑙掕壊
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:role:update")]
    public async Task<Result<object?>> UpdateRole([FromRoute] long id, [FromBody] RoleForm formData, CancellationToken cancellationToken)
    {
        if (formData.Id.HasValue && formData.Id.Value != id)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "瑙掕壊ID涓嶅尮閰?);
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
    /// 鎵归噺鍒犻櫎瑙掕壊
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:role:delete")]
    public async Task<Result<object?>> DeleteRoles([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _roleService.DeleteByIdsAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 淇敼瑙掕壊鐘舵€?
    /// </summary>
    [HttpPut("{roleId:long}/status")]
    [HasPerm("sys:role:update")]
    public async Task<Result<object?>> UpdateRoleStatus([FromRoute] long roleId, [FromQuery] int status, CancellationToken cancellationToken)
    {
        var ok = await _roleService.UpdateRoleStatusAsync(roleId, status, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 瑙掕壊宸插垎閰嶇殑鑿滃崟ID
    /// </summary>
    [HttpGet("{roleId:long}/menuIds")]
    public async Task<Result<IReadOnlyCollection<long>>> GetRoleMenuIds([FromRoute] long roleId, CancellationToken cancellationToken)
    {
        var ids = await _roleService.GetRoleMenuIdsAsync(roleId, cancellationToken);
        return Result.Success(ids);
    }

    /// <summary>
    /// 鍒嗛厤鑿滃崟
    /// </summary>
    [HttpPut("{roleId:long}/menus")]
    [HasPerm("sys:role:assign")]
    public async Task<Result<object?>> AssignMenusToRole([FromRoute] long roleId, [FromBody] List<long> menuIds, CancellationToken cancellationToken)
    {
        await _roleService.AssignMenusToRoleAsync(roleId, menuIds, cancellationToken);
        return Result.Success();
    }
}
