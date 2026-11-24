using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Results;
using Youlai.Application.System.Models.Log;
using Youlai.Application.System;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 系统日志接口（查询）
/// </summary>
[ApiController]
[Route("api/v1/logs")]
[Authorize]
[Tags("09.日志接口")]
public sealed class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 日志分页
    /// </summary>
    [HttpGet]
    [HasPerm("sys:log:list")]
    public async Task<PageResult<LogPageVo>> GetLogPage([FromQuery] LogQuery queryParams, CancellationToken cancellationToken)
    {
        return await _logService.GetLogPageAsync(queryParams, cancellationToken);
    }
}
