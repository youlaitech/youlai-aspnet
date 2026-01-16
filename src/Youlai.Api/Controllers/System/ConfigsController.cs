using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Config;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 鍙傛暟閰嶇疆鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鍙傛暟閰嶇疆鐨勫垎椤垫煡璇€佽鎯呫€佸垱寤恒€佷慨鏀广€佸垹闄ょ瓑鑳藉姏
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
    /// 閰嶇疆鍒嗛〉
    /// </summary>
    [HttpGet]
    [HasPerm("sys:config:list")]
    public async Task<PageResult<ConfigPageVo>> GetConfigPage([FromQuery] ConfigQuery queryParams, CancellationToken cancellationToken)
    {
        return await _configService.GetConfigPageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 閰嶇疆琛ㄥ崟
    /// </summary>
    [HttpGet("{id:long}/form")]
    public async Task<Result<ConfigForm>> GetConfigForm([FromRoute] long id, CancellationToken cancellationToken)
    {
        var form = await _configService.GetConfigFormAsync(id, cancellationToken);
        return Result.Success(form);
    }

    /// <summary>
    /// 鏂板閰嶇疆
    /// </summary>
    [HttpPost]
    [HasPerm("sys:config:create")]
    public async Task<Result<object?>> AddConfig([FromBody] ConfigForm formData, CancellationToken cancellationToken)
    {
        var ok = await _configService.SaveConfigAsync(formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鏇存柊閰嶇疆
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPerm("sys:config:update")]
    public async Task<Result<object?>> UpdateConfig([FromRoute] long id, [FromBody] ConfigForm formData, CancellationToken cancellationToken)
    {
        var ok = await _configService.UpdateConfigAsync(id, formData, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鍒犻櫎閰嶇疆
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPerm("sys:config:delete")]
    public async Task<Result<object?>> DeleteConfig([FromRoute] long id, CancellationToken cancellationToken)
    {
        var ok = await _configService.DeleteConfigAsync(id, cancellationToken);
        return Result.Judge(ok);
    }

    /// <summary>
    /// 鍒锋柊閰嶇疆缂撳瓨
    /// </summary>
    [HttpPut("refresh")]
    [HasPerm("sys:config:refresh")]
    public async Task<Result<object?>> RefreshCache(CancellationToken cancellationToken)
    {
        var ok = await _configService.RefreshCacheAsync(cancellationToken);
        return Result.Judge(ok);
    }
}
