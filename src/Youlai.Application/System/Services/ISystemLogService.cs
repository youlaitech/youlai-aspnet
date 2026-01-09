using Youlai.Application.Common.Results;
using Youlai.Application.System.Dtos;

namespace Youlai.Application.System.Services;

/// <summary>
/// 日志与统计
/// </summary>
public interface ISystemLogService
{
    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    Task<PageResult<LogPageVo>> GetLogPageAsync(LogPageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问趋势
    /// </summary>
    Task<VisitTrendVo> GetVisitTrendAsync(VisitTrendQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问统计
    /// </summary>
    Task<VisitStatsVo> GetVisitStatsAsync(CancellationToken cancellationToken = default);
}
