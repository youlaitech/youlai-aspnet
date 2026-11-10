using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Api.Security;
using Youlai.Application.Results;
using Youlai.Application.System.Models.Log;
using Youlai.Application.System.Models.Statistics;
using Youlai.Application.System;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 系统日志接口（查询、清理）
/// </summary>
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
    [HasPerm("sys:log:list")]
    public async Task<PageResult<LogPageVo>> GetLogPage([FromQuery] LogQuery queryParams, CancellationToken cancellationToken)
    {
        return await _logService.GetLogPageAsync(queryParams, cancellationToken);
    }

    /// <summary>
    /// 访问趋势
    /// </summary>
    [HttpGet("analytics/trend")]
    public async Task<Result<VisitTrendVo>> GetVisitTrend([FromQuery] VisitTrendQuery queryParams, CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitTrendAsync(queryParams, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 访问统计概览
    /// </summary>
    [HttpGet("analytics/overview")]
    public async Task<Result<VisitStatsVo>> GetVisitOverview(CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitStatsAsync(cancellationToken);
        return Result.Success(data);
    }
}
