using Youlai.Application.Results;
using Youlai.Application.System.Models.Log;
using Youlai.Application.System.Models.Statistics;

namespace Youlai.Application.System;

/// <summary>
/// 日志与统计
/// </summary>
public interface ISystemLogService
{
    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    Task<PageResult<LogPageVo>> GetLogPageAsync(LogQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问趋势
    /// </summary>
    Task<VisitTrendVo> GetVisitTrendAsync(VisitTrendQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问统计
    /// </summary>
    Task<VisitStatsVo> GetVisitStatsAsync(CancellationToken cancellationToken = default);
}
