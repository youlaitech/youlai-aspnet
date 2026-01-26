using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Log;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 系统日志接口
/// </summary>
/// <remarks>
/// 提供操作日志查询与清理能力。
/// </remarks>
[ApiController]
[Route("api/v1/logs")]
[Authorize]
[Tags("10.日志接口")]
public sealed class LogsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public LogsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 日志分页
    /// </summary>
    [HttpGet]
    public async Task<PageResult<LogPageVo>> GetLogPage([FromQuery] LogQuery queryParams, CancellationToken cancellationToken)
    {
        return await _logService.GetLogPageAsync(queryParams, cancellationToken);
    }
}
