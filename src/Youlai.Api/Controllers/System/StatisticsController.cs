using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos.Statistics;
using Youlai.Application.System.Services;

namespace Youlai.Api.Controllers.System;

/// <summary>
/// 缁熻鎺ュ彛
/// </summary>
/// <remarks>
/// 鎻愪緵绯荤粺缁熻鏁版嵁鏌ヨ鑳藉姏
/// </remarks>
[ApiController]
[Route("api/v1/statistics")]
[Authorize]
public sealed class StatisticsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public StatisticsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 璁块棶瓒嬪娍
    /// </summary>
    [HttpGet("visits/trend")]
    public async Task<Result<VisitTrendVo>> GetVisitTrend([FromQuery] VisitTrendQuery queryParams, CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitTrendAsync(queryParams, cancellationToken);
        return Result.Success(data);
    }

    /// <summary>
    /// 璁块棶姒傝
    /// </summary>
    [HttpGet("visits/overview")]
    public async Task<Result<VisitStatsVo>> GetVisitOverview(CancellationToken cancellationToken)
    {
        var data = await _logService.GetVisitStatsAsync(cancellationToken);
        return Result.Success(data);
    }
}
