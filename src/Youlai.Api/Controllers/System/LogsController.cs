using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Log;
using Youlai.Application.System.Dtos.Statistics;
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
[Tags("09.日志接口")]
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

    /// <summary>
    /// 访问趋势
    /// </summary>
    [HttpGet("views/trend")]
    [AllowAnonymous]
    public async Task<Result<VisitTrendVo>> GetVisitTrend([FromQuery] VisitTrendQuery queryParams, CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitTrendAsync(queryParams, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 访问统计概览
    /// </summary>
    [HttpGet("views")]
    [AllowAnonymous]
    public async Task<Result<VisitStatsVo>> GetVisitOverview(CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitStatsAsync(cancellationToken);
        return Result.Success(data);
    }
}
