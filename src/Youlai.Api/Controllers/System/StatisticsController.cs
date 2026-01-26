using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Statistics;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 统计接口
/// </summary>
/// <remarks>
/// 提供系统统计数据查询能力。
/// </remarks>
[ApiController]
[Route("api/v1/statistics")]
[Authorize]
[Tags("12.统计接口")]
public sealed class StatisticsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public StatisticsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 访问趋势
    /// </summary>
    [HttpGet("visits/trend")]
    public async Task<Result<VisitTrendVo>> GetVisitTrend([FromQuery] VisitTrendQuery queryParams, CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitTrendAsync(queryParams, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 访问概览
    /// </summary>
    [HttpGet("visits/overview")]
    public async Task<Result<VisitStatsVo>> GetVisitOverview(CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitStatsAsync(cancellationToken);
        return Result.Success(data);
    }
}
