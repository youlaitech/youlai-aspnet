using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Log;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 绯荤粺鏃ュ織鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵鎿嶄綔鏃ュ織鏌ヨ涓庢竻鐞嗚兘鍔?
/// </remarks>
[ApiController]
[Route("api/v1/logs")]
[Authorize]
public sealed class LogsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public LogsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 鏃ュ織鍒嗛〉
    /// </summary>
    [HttpGet]
    public async Task<PageResult<LogPageVo>> GetLogPage([FromQuery] LogQuery queryParams, CancellationToken cancellationToken)
    {
        return await _logService.GetLogPageAsync(queryParams, cancellationToken);
    }
}
