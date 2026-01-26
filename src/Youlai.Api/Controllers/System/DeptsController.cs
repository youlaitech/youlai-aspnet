using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Dept;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 部门管理接口
/// </summary>
/// <remarks>
/// 提供部门树查询、详情、创建、修改、删除等能力。
/// </remarks>
[ApiController]
[Route("api/v1/depts")]
[Authorize]
[Tags("05.部门接口")]
public sealed class DeptsController : ControllerBase
{
    private readonly ISystemDeptService _deptService;

    public DeptsController(ISystemDeptService deptService)
    {
        _deptService = deptService;
    }

    /// <summary>
    /// 部门列表
    /// </summary>
    [HttpGet]
    public async Task<Result<IReadOnlyCollection<DeptVo>>> GetDeptList([FromQuery] DeptQuery queryParams, CancellationToken cancellationToken)
    {
        var list = await _deptService.GetDeptListAsync(queryParams, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 部门下拉选项
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetDeptOptions(CancellationToken cancellationToken)
    {
        var list = await _deptService.GetDeptOptionsAsync(cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 新增部门
    /// </summary>
    [HttpPost]
    [HasPerm("sys:dept:create")]
    public async Task<Result<long>> SaveDept([FromBody] DeptForm formData, CancellationToken cancellationToken)
    {
        var id = await _deptService.SaveDeptAsync(formData, cancellationToken);
        return Result.Success(id);
    }

    /// <summary>
    /// 部门表单
    /// </summary>
    [HttpGet("{deptId:long}/form")]
    public async Task<Result<DeptForm>> GetDeptForm([FromRoute] long deptId, CancellationToken cancellationToken)
    {
        var form = await _deptService.GetDeptFormAsync(deptId, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    [HttpPut("{deptId:long}")]
    [HasPerm("sys:dept:update")]
    public async Task<Result<long>> UpdateDept([FromRoute] long deptId, [FromBody] DeptForm formData, CancellationToken cancellationToken)
    {
        var id = await _deptService.UpdateDeptAsync(deptId, formData, cancellationToken);
        return Result.Success(id);
    }

    /// <summary>
    /// 批量删除部门
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:dept:delete")]
    public async Task<Result<object?>> DeleteDepartments([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _deptService.DeleteByIdsAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }
}
