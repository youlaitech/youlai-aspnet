using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Results;
using Youlai.Application.System;
using Youlai.Application.System.Models.Statistics;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 统计接口
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Authorize]
[Tags("10.统计接口")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ILogService _logService;

    public AnalyticsController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 访问汇总
    /// </summary>
    [HttpGet("visits/summary")]
    public async Task<Result<VisitSummaryVo>> GetVisitSummary(CancellationToken cancellationToken)
    {
        return Result.Success(await _logService.GetVisitSummaryAsync(cancellationToken));
    }

    /// <summary>
    /// 访问趋势
    /// </summary>
    [HttpGet("visits/trend")]
    public async Task<Result<VisitTrendVo>> GetVisitTrend([FromQuery] VisitTrendQuery queryParams, CancellationToken cancellationToken)
    {
        return Result.Success(await _logService.GetVisitTrendAsync(queryParams, cancellationToken));
    }

    /// <summary>
    /// 用户汇总
    /// </summary>
    [HttpGet("users/summary")]
    public async Task<Result<UserSummaryVo>> GetUserSummary(CancellationToken cancellationToken)
    {
        return Result.Success(await _logService.GetUserSummaryAsync(cancellationToken));
    }
}
