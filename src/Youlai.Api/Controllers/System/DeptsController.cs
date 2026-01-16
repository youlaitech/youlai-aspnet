using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Dept;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 閮ㄩ棬绠＄悊鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵閮ㄩ棬鏍戞煡璇€佽鎯呫€佸垱寤恒€佷慨鏀广€佸垹闄ょ瓑鑳藉姏
/// </remarks>
[ApiController]
[Route("api/v1/depts")]
[Authorize]
public sealed class DeptsController : ControllerBase
{
    private readonly ISystemDeptService _deptService;

    public DeptsController(ISystemDeptService deptService)
    {
        _deptService = deptService;
    }

    /// <summary>
    /// 閮ㄩ棬鍒楄〃
    /// </summary>
    [HttpGet]
    public async Task<Result<IReadOnlyCollection<DeptVo>>> GetDeptList([FromQuery] DeptQuery queryParams, CancellationToken cancellationToken)
    {
        var list = await _deptService.GetDeptListAsync(queryParams, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 閮ㄩ棬涓嬫媺閫夐」
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<long>>>> GetDeptOptions(CancellationToken cancellationToken)
    {
        var list = await _deptService.GetDeptOptionsAsync(cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 鏂板閮ㄩ棬
    /// </summary>
    [HttpPost]
    [HasPerm("sys:dept:create")]
    public async Task<Result<long>> SaveDept([FromBody] DeptForm formData, CancellationToken cancellationToken)
    {
        var id = await _deptService.SaveDeptAsync(formData, cancellationToken);
        return Result.Success(id);
    }

    /// <summary>
    /// 閮ㄩ棬琛ㄥ崟
    /// </summary>
    [HttpGet("{deptId:long}/form")]
    public async Task<Result<DeptForm>> GetDeptForm([FromRoute] long deptId, CancellationToken cancellationToken)
    {
        var form = await _deptService.GetDeptFormAsync(deptId, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 鏇存柊閮ㄩ棬
    /// </summary>
    [HttpPut("{deptId:long}")]
    [HasPerm("sys:dept:update")]
    public async Task<Result<long>> UpdateDept([FromRoute] long deptId, [FromBody] DeptForm formData, CancellationToken cancellationToken)
    {
        var id = await _deptService.UpdateDeptAsync(deptId, formData, cancellationToken);
        return Result.Success(id);
    }

    /// <summary>
    /// 鎵归噺鍒犻櫎閮ㄩ棬
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:dept:delete")]
    public async Task<Result<object?>> DeleteDepartments([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _deptService.DeleteByIdsAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }
}
