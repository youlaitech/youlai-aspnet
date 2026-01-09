using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers;

/// <summary>
/// 参数配置接口
/// </summary>
/// <remarks>
/// 提供参数配置的分页查询、详情、创建、修改、删除等能力
/// </remarks>
[ApiController]
[Route("api/v1/configs")]
[Authorize]
public sealed class ConfigsController : ControllerBase
{
    private readonly ISystemConfigService _configService;

    public ConfigsController(ISystemConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 配置分页
    /// </summary>
    [HttpGet]
    [HasPerm("sys:config:list")]
    public async Task<PageResult<ConfigPageVo>> GetConfigPage([FromQuery] ConfigPageQuery queryParams, CancellationToken cancellationToken)
    {
        return await _configService.GetConfigPageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 配置表单
    /// </summary>
    [HttpGet("{id:long}/form")]
    public async Task<Result<ConfigForm>> GetConfigForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _configService.GetConfigFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 新增配置
    /// </summary>
    [HttpPost]
    [HasPerm("sys:config:create")]
    public async Task<Result<object?>> AddConfig([FromBody] ConfigForm formData, CancellationToken cancellationToken)
    {
        var ok = await _configService.SaveConfigAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:config:update")]
    public async Task<Result<object?>> UpdateConfig([FromRoute] long id, [FromBody] ConfigForm formData, CancellationToken cancellationToken)
    {
        var ok = await _configService.UpdateConfigAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPerm("sys:config:delete")]
    public async Task<Result<object?>> DeleteConfig([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _configService.DeleteConfigAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 刷新配置缓存
    /// </summary>
    [HttpPut("refresh")]
    [HasPerm("sys:config:refresh")]
    public async Task<Result<object?>> RefreshCache(CancellationToken cancellationToken)
    {
        var ok = await _configService.RefreshCacheAsync(cancellationToken);
        return Result.Judge(ok);
    }
}
