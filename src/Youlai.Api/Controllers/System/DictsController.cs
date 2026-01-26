using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Models;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Dict;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 数据字典接口
/// </summary>
/// <remarks>
/// 提供字典类型与字典项的查询、维护及变更通知能力。
/// </remarks>
[ApiController]
[Route("api/v1/dicts")]
[Authorize]
public sealed class DictsController : ControllerBase
{
    private readonly ISystemDictService _dictService;

    public DictsController(ISystemDictService dictService)
    {
        _dictService = dictService;
    }

    /// <summary>
    /// 字典分页
    /// </summary>
    [HttpGet]
    public Task<PageResult<DictPageVo>> GetDictPage([FromQuery] DictQuery query, CancellationToken cancellationToken)
    {
        return _dictService.GetDictPageAsync(query, cancellationToken);
    }

    /// <summary>
    /// 字典下拉选项
    /// </summary>
    [HttpGet("options")]
    public async Task<Result<IReadOnlyCollection<Option<string>>>> GetDictList(CancellationToken cancellationToken)
    {
        var list = await _dictService.GetDictListAsync(cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 字典表单
    /// </summary>
    [HttpGet("{id:long}/form")]
    public async Task<Result<DictForm>> GetDictForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _dictService.GetDictFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 新增字典
    /// </summary>
    [HttpPost]
    [HasPerm("sys:dict:create")]
    public async Task<Result<object?>> CreateDict([FromBody] DictForm formData, CancellationToken cancellationToken)
    {
        var ok = await _dictService.CreateDictAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 更新字典
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:dict:update")]
    public async Task<Result<object?>> UpdateDict([FromRoute] long id, [FromBody] DictForm formData, CancellationToken cancellationToken)
    {
        var ok = await _dictService.UpdateDictAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 批量删除字典
    /// </summary>
    [HttpDelete("{ids}")]
    [HasPerm("sys:dict:delete")]
    public async Task<Result<object?>> DeleteDicts([FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _dictService.DeleteDictsAsync(ids, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 字典项分页
    /// </summary>
    [HttpGet("{dictCode}/items")]
    public Task<PageResult<DictItemPageVo>> GetDictItemPage([FromRoute] string dictCode, [FromQuery] DictItemQuery query, CancellationToken cancellationToken)
    {
        return _dictService.GetDictItemPageAsync(dictCode, query, cancellationToken);
    }

    /// <summary>
    /// 字典项列表
    /// </summary>
    [HttpGet("{dictCode}/items/options")]
    public async Task<Result<IReadOnlyCollection<DictItemOption>>> GetDictItems([FromRoute] string dictCode, CancellationToken cancellationToken)
    {
        var list = await _dictService.GetDictItemsAsync(dictCode, cancellationToken);
        return Result.Success(list);
    }

    /// <summary>
    /// 新增字典项
    /// </summary>
    [HttpPost("{dictCode}/items")]
    [HasPerm("sys:dict-item:create")]
    public async Task<Result<object?>> CreateDictItem([FromRoute] string dictCode, [FromBody] DictItemForm formData, CancellationToken cancellationToken)
    {
        var ok = await _dictService.CreateDictItemAsync(dictCode, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 字典项表单
    /// </summary>
    [HttpGet("{dictCode}/items/{id:long}/form")]
    public async Task<Result<DictItemForm>> GetDictItemForm([FromRoute] string dictCode, [FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _dictService.GetDictItemFormAsync(dictCode, id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 更新字典项
    /// </summary>
    [HttpPut("{dictCode}/items/{id:long}")]
    [HasPerm("sys:dict-item:update")]
    public async Task<Result<object?>> UpdateDictItem([FromRoute] string dictCode, [FromRoute] long id, [FromBody] DictItemForm formData, CancellationToken cancellationToken)
    {
        var ok = await _dictService.UpdateDictItemAsync(dictCode, id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 批量删除字典项
    /// </summary>
    [HttpDelete("{dictCode}/items/{ids}")]
    [HasPerm("sys:dict-item:delete")]
    public async Task<Result<object?>> DeleteDictItems([FromRoute] string dictCode, [FromRoute] string ids, CancellationToken cancellationToken)
    {
        var ok = await _dictService.DeleteDictItemsAsync(dictCode, ids, cancellationToken);
        return Result.Judge(ok);
    }
}
